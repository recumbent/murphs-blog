---
layout: post
title: Improving my fornax blog
author: @recumbent
tags: how-to fornax f#
published: 2020-05-03
---
# Improving my fornax blog

The most obvious problem I have at the moment is aesthetics - this site could look better (and the content needs to be better presented on the home page). But of course that's not actually the thing that bothers me most.

I have views on what I want the structure of the site to be, and the structure I have is nowhere near and whilst I can (and will) add content and I can (and will) improve the appearence getting the structure right bothers me.

## Lightbulb moments

One of the "fun" things about programming is that enlightenment can come when you don't expect it - there's a notion of always carrying a notebook (nowadays we have a phone) so that when you work out how to do something you can write it down so you don't forget (I remember having this problem exactly halfway between Beit Hall and Southside Bar).

In this case what I worked out was that I was trying to solve the wrong problem or rather that I was looking at the wrong end of the tooling to solve my problem

## Where am I now and where do I want to go.

Currently I have something like this:

* index.html
* posts
    * 2020-01-01-Post-one.html
    * 2020-02-01-Post-two.html
    * 2020-03-01-Post-three.html
    * 2020-03-08-Post-four.html

But what I acutally want is somethig more like:

* index.html
* posts
  * 2020
    * index.html
    * 01
      * index.html
      * 01-Post-one.html
    * 02
      * index.html
      * 01-Post-two.html
    * 03
      * index.html
      * 01-Post-three.html
      * 08-Post-four.html
* tags
  * index.html
  * fornax.html
  * fornax.html
  * fsharp.html

The aim being to have a wholly static site still but to have a discoverable URL structure. I probably don't want to go all the way down to a folder per day as I really don't expect to be churning out many pages per month and very very rarely do I expect to publish more than one per day.

On top of the above challenges, I also need to think about an RSS feed and other things.

## So what did I learn

~~If I've got this right, then the flaw in my thinking was that I was looking at how to _generate_ the additional pages I wanted not how to _load_ them.~~

I didn't get that right... taking a step back, fornax is built on a two stage process, first you load the content you want to generate and then from that content you generate the pages you need. I was attemping to work out how to generate more pages than I'd loaded, so the first answer is to look to load pages that don't actually exist (and get a bit more creative with file naming). Unfortunately that's not quite right either (though there was value in the though process).

So at this point I went to look at the source code - both for fornax itself and for sites built with fornax (e.g. [Saturn Docs](https://github.com/SaturnFramework/Saturn/tree/master/docs)).

So the fundamental challenge is that basis for generatation is this:

```fsharp
        Directory.GetFiles(projectRoot, "*", SearchOption.AllDirectories)
        |> Array.iter (fun filePath ->
            filePath
            |> relative projectRoot
            |> generate fsi config sc projectRoot
            |> logResult)
```

i.e. a loop over all the from the project root on down. It doesn't then matter what you loaded, the generation process is driven by the content of the folders.

Fortunately (for me) this turns out to have been addressed by having a generator output type of `MultipleFiles` which appears in the list of generators as something like:

```fsharp
        { Script = "yearindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "posts/%s/index.html") }
```

And the signature of the generate function changes too from `ctx:SiteContents ->  projectRoot:string -> page:string -> string` to `ctx:SiteContents ->  projectRoot:string -> page:string -> (string * string) list` - so instead of outputing a single generated string (i.e. one output file), we're generating a list of tuples where the first value is a name and the second the generated string. 

## Yes, and? Show me the codes...

Armed with this new capability there are two ways one might approach this, one could attempt to work out the details from the SiteContent loaded for other purposes, or one could explicitly create site content as we go. The latter allows for use of the same content in other contexts, so I decided to go with that (well I might have decided on that before discovering that I hadn't understood... but its still valid so we'll just go with it anyway).

First thing we need to do is to create a new type of content:

```fsharp
type YearIndex = {
  file: string
  year: int
}

type MonthIndex = {
  file: string
  year: int
  month: int
}
```

There are probably better ways to do this, but this is a place to start

Next step is to add those into the site content as needed - when we load a post add the year and the month if we haven't got them already:

```fsharp
let processYearIndex (siteContent: SiteContents) (date: DateTime) =
    let yearExists = 
        siteContent.TryGetValues<YearIndex>() 
        |> Option.defaultValue Seq.empty
        |> Seq.exists (fun yi -> yi.year = date.Year)

    if (not yearExists) then 
        let yi: YearIndex = { file = (sprintf "%04i/index.html" date.Year); year = date.Year}
        siteContent.Add yi

let processMonthIndex (siteContent: SiteContents) (date: DateTime) =
    let monthExists = 
        siteContent.GetValues<MonthIndex>() 
        |> Seq.exists (fun mi -> mi.year = date.Year && mi.month = date.Month)

    if (not monthExists) then 
        let mi: MonthIndex = { file = (sprintf "%04i/%02i/index.html" date.Year date.Month); year = date.Year; month = date.Month}
        siteContent.Add mi
```

Because I'm now doing three different things for each loaded post, I'll add a new function to carefully do all three and change the lambda in the `loader` function to call that instead of just adding the post to the site context. Net result looks like this:

```fsharp
let processPost (siteContent: SiteContents) (post: Post) =
    siteContent.Add post

    match post.published with
    | Some date -> 
        processYearIndex siteContent date
        processMonthIndex siteContent date
    | None -> ()
    

let loader (projectRoot: string) (siteContent: SiteContents) =
    let postsPath = System.IO.Path.Combine(projectRoot, "posts")
    System.IO.Directory.GetFiles postsPath
    |> Array.filter (fun n -> n.EndsWith ".md")
    |> Array.map loadFile
    |> Array.iter (fun p -> processPost siteContent p)

    siteContent.Add({disableLiveRefresh = true})
    siteContent
```

## Generating multiple pages

The above has dealt with loading the data, so now we need to generate the index pages.

As always we start by adding a new file to `generators` in this case `monthindex.fsx`.

Working bottom up...

The `generate` and `generate'` functions looks like this:

```fsharp
let generate' (ctx : SiteContents) (page: string) =
    let months =
        ctx.TryGetValues<Postloader.MonthIndex> ()
        |> Option.defaultValue Seq.empty

    months 
    |> Seq.map (generateMonth ctx) 
    |> Seq.toList

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> List.map (fun (n,b) -> n, (Layout.render ctx b))
```

In this we get all the months from `SiteContents` and, assuming any exist, we call a function that takes the context and a specific month and creates content for that month.

That function is a bit more involved and looks like this:

```fsharp
let generateMonth (ctx : SiteContents) (month: Postloader.MonthIndex) =
    let filter year month (date: DateTime option) = 
        let date = Option.defaultValue DateTime.MinValue date
        date.Year = year && date.Month = month

    let posts = 
        ctx.TryGetValues<Postloader.Post> () 
        |> Option.defaultValue Seq.empty
        |> Seq.filter (fun p -> filter month.year month.month p.published)

    let published (post: Postloader.Post) =
        post.published
        |> Option.defaultValue System.DateTime.MinValue
        |> fun n -> n.ToString("yyyy-MM-dd")

    let postList =
        posts
        |> Seq.sortByDescending published
        |> Seq.toList
        |> List.map (fun post ->
            li [] [
                a [Href post.link] [!! (sprintf "%s - %s" (published post) post.title)]
            ]
        )

    let title = sprintf "%04i-%02i" month.year month.month
    let name =  sprintf "%04i/%02i" month.year month.month
    
    let monthContent = Layout.layout ctx title  [
        section [] [
            h2 [] [!! (sprintf "Posts for %s" title)]
            ul [] postList
        ]
    ]

    name, monthContent
```

In this we first create a filtered list of posts that are published in the selected month, then map those to a sorted list of list elements before finally creating the actual content for the page - dropping the list elements into an unordered list.

We then return a "name" - in this case the year and the month formatted to make the folder structure work and the content for the month (which will get wrapped in a standard page layout).

Looking at the above there are several opporunities to refactor out common elements - like formatting the published date and, indeed, the whole list of links to posts.

If one avoids being dry (for now), then the equivalent by year `yearindex` is very similar and looks like:

```fsharp
#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open System
open Html

let generateYears (ctx : SiteContents) (year: Postloader.YearIndex) =
    let filter year (date: DateTime option) = 
        let date = Option.defaultValue DateTime.MinValue date
        date.Year = year

    let posts = 
        ctx.TryGetValues<Postloader.Post> () 
        |> Option.defaultValue Seq.empty
        |> Seq.filter (fun p -> filter year.year p.published)

    let published (post: Postloader.Post) =
        post.published
        |> Option.defaultValue System.DateTime.MinValue
        |> fun n -> n.ToString("yyyy-MM-dd")

    let postList =
        posts
        |> Seq.sortByDescending published
        |> Seq.toList
        |> List.map (fun post ->
            li [] [
                a [Href post.link] [!! (sprintf "%s - %s" (published post) post.title)]
            ]
        )

    let title = sprintf "%04i" year.year
    
    let monthContent = Layout.layout ctx title  [
        section [] [
            h2 [] [!! (sprintf "Posts for %s" title)]
            ul [] postList
        ]
    ]

    title, monthContent

let generate' (ctx : SiteContents) (page: string) =
    let years =
        ctx.TryGetValues<Postloader.YearIndex> ()
        |> Option.defaultValue Seq.empty

    years 
    |> Seq.map (generateYears ctx) 
    |> Seq.toList

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> List.map (fun (n,b) -> n, (Layout.render ctx b))
```

A future iteration on this would add grouping by months as within the year, just to be tidy.

## Adding years and months to config

Having created generators to output the required content, the last step is to wire those into `config.fsx`

To add the index by month we need to add the following to the list of generators:

```fsharp
        { Script = "monthindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "posts/%s/index.html") }
```

This tells fornax to run the `monthindex.fsx` generator once, that the OutputFile is _multiple_ files - the list of tuples returned from the generate function - and that the file name for each tuple is created using the specified function.

And that takes me another step toward where I want to be.