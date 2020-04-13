---
layout: post
title: Building up fornax from scratch
author: @recumbent
tags: how-to
published: 2020-04-12
---
# Setting up fornax from scratch

I like F#, I want a static site generator - so as I write this there is an obvious answer to address my needs: [Fornax](https://github.com/ionide/Fornax) - easy right? Well... maybe and then again maybe not.

## Why not?

I think that its reasonable to be opinionated, I think its reasonable to provide a good example. I'm rather less convinced by the nature of "starter" apps that I've been seeing for a while (and this goes back to file | new project... for ASP.NET MVC applications)

Fornax is interesting - the latest incarnation makes some choices that mean you're going to need some guidance in making it work (you're going to _want_ to start with the generated site) but equally there's too much in there if you just want a bare-bones setup to build the way you choose. Specifically the template uses bulma, pulls in a hero image, etc, etc where I don't want to use bulma (at least not in the first instance), I haven't even thought about complex aesthetics, but I do want to push content.

So I'm going to try this a different way - I've got the templated site, I can rebuild from the ground up by pulling the pieces I need from that site as I need them - hopefully learning as I go.

## Where to start

First install the tooling.

Assuming [the .NET Core 3.1 SDK](https://dotnet.microsoft.com/download) is available:

```sh
dotnet new tool-manifest
dotnet tool install fornax
```

That gives us a `.config` folder, and a `dotnet-tools.json` therein and an ability to run fornax e.g.

```sh
dotnet fornax version
```

## Create a new site and throw it almost all away...

Run:

```
dotnet fornax new
```

Now delete everything except the `_lib` folder.

Finally re-create `config.fsx` to look like the following:

```fsharp
#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let config = {
    Generators = [
        {Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
    ]
}
```

Which throws away almost everything... (we're going to want it back later, but that will do for now)

Lets run that...

```sh
dotnet fornax watch
```

And it will fail gloriously because it can find a loader. So lets add one of those.

create a folder `loaders` and in that create a file `pageloader.fsx` containing the following

```fsharp
#r "../_lib/Fornax.Core.dll"

type Page = {
    title: string
    link: string
}

let loader (projectRoot: string) (siteContent: SiteContents) =
    siteContent.Add({title = "Home"; link = "/"})

    siteContent
```

And generation will fail because there's no index generator, so we create a new folder `generators` and in that we need a file `index.fsx`

```fsharp
#r "../_lib/Fornax.Core.dll"

open Html

let render (ctx : SiteContents) content =
    content
    |> HtmlElement.ToString

let generate' (ctx : SiteContents) (_: string) =
    html [] [
        head [] [
            meta [CharSet "utf-8"]
            title [] [!! "Almost heading for a blog"]
        ]
        body [] [
            header [] [!! "My Blog has a title!!!"]
            section [] []
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> render ctx
```

And, if we run `dotnet fornax watch`, magic happens, files get built, a webserver starts and end up with a single web page at http://127.0.0.1:8080

At this point `dotnet fornax clean` may be useful!

## Its a blog... where are the posts?

Lets add a couple of posts, create a folder `posts` and add something like the following two files:

`post-01.md`:

```md
---
layout: post
title: First Post
published: 2020-01-01
author: @recumbent
---

This is the first post
```

`post-02.md`:

```md
---
layout: post
title: Second Post
published: 2020-02-01
author: @recumbent
---

This is the second post

It has two paragraphs
```

So the next trick is to find those post files and list their titles in the index page.

## Loading posts

We need a post loader, at this point we want _just enough_ to be able to create a list of posts - so we need to add a file `postloader.fsx` in the `loaders` folder with content as follows:

```fsharp
#r "../_lib/Fornax.Core.dll"

type PostConfig = {
    disableLiveRefresh: bool
}

type Post = {
    title: string
}

let trimString (str : string) =
    str.Trim().TrimEnd('"').TrimStart('"')

let loadFile n =
    let text = System.IO.File.ReadAllText n

    let lines = text.Split( '\n') |> List.ofArray

    let title = lines |> List.find (fun n -> n.ToLower().StartsWith "title" ) |> fun n -> n.Split(':').[1] |> trimString

    { title = title }

let loader (projectRoot: string) (siteContent: SiteContents) =
    let postsPath = System.IO.Path.Combine(projectRoot, "posts")
    System.IO.Directory.GetFiles postsPath
    |> Array.filter (fun n -> n.EndsWith ".md")
    |> Array.map loadFile
    |> Array.iter (fun p -> siteContent.Add p)

    siteContent.Add({disableLiveRefresh = false})
    siteContent
```

There's a bit going on here:

* First we define a type `posts` to contain the title - we'll expand this later
* Then we have a utility function `trimString`
* Then we have a very basic `loadFile` function that reads all the text, splits it by line, and goes to find the title (defined in the front matter of the post)
* Finally we have the loader itself, which goes looking for `.md` files in the "posts" folder, loads them, and then adds them to the site content (side effects, that's not very functional!) 

This is sufficient to see the mechanics.

Now we need to update the generator to create a list from the pages, so change the `generate'` function in `generators\index.fsx` to:

```fsharp
let generate' (ctx : SiteContents) (_: string) =
    let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty

    let postList =
        posts
        |> Seq.toList
        |> List.map (fun post ->
            li [] [!! post.title]
        )

    html [] [
        head [] [
            meta [CharSet "utf-8"]
            title [] [!! "Almost heading for a blog"]
        ]
        body [] [
            h1 [] [!! "My Blog has a title!!!"]
            section [] [
                ul [] postList
            ]
        ]
    ]
```

Now when the site is built we'll have an index page similar to the following (but probably less pretty as it will be using your browser's default styles):

---

<h1>
    My Blog has a title!!!
</h1>
<section>
    <ul>
    <li>
        First Post
    </li>
    <li>
        Second Post
    </li>
    </ul>
</section>

---

## Adding a page per post

The next step is to generate a page for every post and to link from the list in the index page to those pages.

We want to have a consistent layout, so lets add `layout.fsx` in the `generators` folder.

```fsharp
#r "../_lib/Fornax.Core.dll"

open Html

let injectWebsocketCode (webpage:string) =
    let websocketScript =
        """
        <script type="text/javascript">
          var wsUri = "ws://localhost:8080/websocket";
      function init()
      {
        websocket = new WebSocket(wsUri);
        websocket.onclose = function(evt) { onClose(evt) };
      }
      function onClose(evt)
      {
        console.log('closing');
        websocket.close();
        document.location.reload();
      }
      window.addEventListener("load", init, false);
      </script>
        """
    let head = "<head>"
    let index = webpage.IndexOf head
    webpage.Insert ( (index + head.Length + 1),websocketScript)

let layout (ctx : SiteContents) active bodyContent =
    html [] [
        head [] [
            meta [CharSet "utf-8"]
            title [] [!! "Almost heading for a blog"]
        ]
        body [] [
            header [] [
              h1 [] [!! "Fornax Generated Blog!"]
              a [Href "/"][!! "Home"]
            ] 
            yield! bodyContent
        ]
    ]

let render (ctx : SiteContents) content =
  let disableLiveRefresh = ctx.TryGetValue<Postloader.PostConfig> () |> Option.map (fun n -> n.disableLiveRefresh) |> Option.defaultValue false
  content
  |> HtmlElement.ToString
  |> fun n -> if disableLiveRefresh then n else injectWebsocketCode n
```

In the above we've taken the bits that we want to be common from the index.fsx page and moved them into their own file, we'll update index in a moment, but first lets load some additional information about posts - for this we need to update the `postloader.fsx`

First add some more fields to the post type:

```fsharp
type Post = {
    file: string
    link : string
    layout: string
    title: string
    author: string option
    published: System.DateTime option
    tags: string list
    content: string
}
```

Next add the folllowing after the post type:

```fsharp
let isSeparator (input : string) =
    input.StartsWith "---"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns front matter configuration
let getConfig (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator

    fileContent
    |> Array.splitAt indexOfSeperator
    |> fst
    |> String.concat "\n"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns body of content for the page
let getContent (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    let _, content = fileContent |> Array.splitAt indexOfSeperator

    content |> Array.skip 1 |> String.concat "\n"
```

_n.b. I'm fighting the urge to refactor the above, but it will work well as is, so that's for another day._

Finally we change the `loadFile` function to the following:

```fsharp
let loadFile n =
    let text = System.IO.File.ReadAllText n

    let config = (getConfig text).Split( '\n') |> List.ofArray

    let content = getContent text

    let file = System.IO.Path.Combine("posts", (n |> System.IO.Path.GetFileNameWithoutExtension) + ".md").Replace("\\", "/")
    let link = "/" + System.IO.Path.Combine("posts", (n |> System.IO.Path.GetFileNameWithoutExtension) + ".html").Replace("\\", "/")

    let title = config |> List.find (fun n -> n.ToLower().StartsWith "title" ) |> fun n -> n.Split(':').[1] |> trimString
    let layout = config |> List.tryFind (fun n -> n.ToLower().StartsWith "layout") |> Option.defaultValue "unknown"

    let published =
        try
            config |> List.tryFind (fun n -> n.ToLower().StartsWith "published" ) |> Option.map (fun n -> n.Split(':').[1] |> trimString |> System.DateTime.Parse)
        with
        | _ -> None

    { file = file
      link = link
      layout = layout
      title = title
      author = None
      published = published
      tags = []
      content = content }
```

The changes above start to add configuration read from the header block in the post files.

Now change index.fsx to look like this:

```fsharp
#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =
    let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty

    let published (post: Postloader.Post) =
        post.published
        |> Option.defaultValue System.DateTime.Now
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

    Layout.layout ctx "Home" [
        section [] [
            h2 [] [!! "Posts:"]
            ul [] postList
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx
```

This pushed the common layout out, adds links to the files for the posts, and generally drifts towards something a bit more real (strictly we want to render at least some of the posts on this page, but for now this lets us show that we're generating all the things)

We next need something to render a blog post, so we'll a new file to the `generators` folder `post.fsx` with the following content:

```fsharp
#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =
    let post = 
        ctx.TryGetValues<Postloader.Post> ()
        |> Option.defaultValue Seq.empty
        |> Seq.find (fun n -> n.file = page)

    Layout.layout ctx post.title [
        article [] [!! post.content]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx
```

This uses the shared layout to drop the content loaded for the post into a page.

Finally add posts to the list of generators in `config.fsx` so it looks like this:

```fsharp
#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let postPredicate (projectRoot: string, page: string) =
    let fileName = Path.Combine(projectRoot, page)
    let ext = Path.GetExtension page
    if ext = ".md" then
        let ctn = File.ReadAllText fileName
        ctn.Contains("layout: post")
    else
        false

let config = {
    Generators = [
        {Script = "post.fsx"; Trigger = OnFilePredicate postPredicate; OutputFile = ChangeExtension "html" }
        {Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
    ]
}
```

The `postPredicate` function lets us find files that we want to render as a post, magic happens here to sweep those files up before we get to the index. In getting to this point I've had some entertainment - but mostly because I had other things in my repo...

So where are we? At this point we can:

* Load content from files
* Parse configuration information from those files
* Generate pages using a script - either based on the content of a file or statically but using information loaded from the files.
* Share layout and other behaviour.

## What have I forgotten

Having got this far I almost have a clue and we almost have something useful... but there are at least a couple more things to do.

Lets start by adding post-03.md as follows:

```markdown
---
layout: post
title: Third Post
published: 2020-03-01
author: @recumbent
---

# This is the 3rd Post

In which we write markdown with _formatting_ including things like this list:

1. One
2. Two
3. Three

And a link: [Fornax on ionide site](http://ionide.io/Tools/fornax.html)
```

When we run `dotnet fornax watch` now we'll get a page - but when we go visit the page we won't see nicely formatted html, but rather something like:

    # This is the 3rd Post In which we write markdown with _formatting_ including things like this list: 1. One 2. Two 3. Three And a link: [Fornax on ionide site](http://ionide.io/Tools/fornax.html)

So we need to add something to convert our markdown into HTML. We'll do that in the loader.

That turns out to be quite straightforward

In `postloader.fsx` first add the following below `#r "../_lib/Fornax.Core.dll"`

```fsharp
#r "../_lib/Markdig.dll"

open Markdig
```

Next add the following after the declaration of the `Post` type:

```fsharp
let markdownPipeline =
    MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseGridTables()
        .Build()
```

And finally change the `getContent` function to:

```fsharp
let getContent (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    let _, content = fileContent |> Array.splitAt indexOfSeperator

    let content = content |> Array.skip 1 |> String.concat "\n"
    Markdown.ToHtml(content, markdownPipeline)
```

The change is in the last two lines where instead of returning the content as read from the file we return the result translated to html by Markdig (if you wanted to use an alternative renderer you'd put it here or hereabouts)

Now if you save the content from post-03 will render html - which is what we want.

## It could do with a bit more style

Well _any_ style really, serif fonts are not really what one expects any more.

In the defaut site the styling is done with bulma, but lets just run with a seriously minimal improvement

create a folder `css` and add a file `styles.css`

```css
body {
    font-family: sans-serif;
}
```

While we're playing lets add a favicon - you can make one here: [FontIcon 💙 Font Awesome Favicon Generator 🔥](https://gauger.io/fonticon/)

Create an `images` folder and save it there

Modify the `layout` function in `layout.fsx` adding the to links to the header so that it now looks like this:

```fsharp
let layout (ctx : SiteContents) active bodyContent =
    html [] [
        head [] [
            meta [CharSet "utf-8"]
            title [] [!! "Almost heading for a blog"]
            link [Rel "icon"; Type "image/png"; Sizes "32x32"; Href "/images/favicon.png"]
            link [Rel "stylesheet"; Type "text/css"; Href "/css/styles.css"]
        ]
        body [] [
            header [] [
              h1 [] [!! "Fornax Generated Blog!"]
              a [Href "/"][!! "Home"]
            ] 
            yield! bodyContent
        ]
    ]
```

We have the files, we're using them, now we need to copy these static files into the published website (i.e. the `_public` folder). The generator this is fairly minimal - create a new file in `generators` called `staticfile.fsx`:

```fsharp
#r "../_lib/Fornax.Core.dll"

open System.IO

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let inputPath = Path.Combine(projectRoot, page)
    File.ReadAllText inputPath
```

This done we need to add the staticfile generator to `config.fsx` but we don't want to copy _everything_ below the root as a static item so we also need need a filter to exclude the various things that are just used to create the site and not as part of it. Making these changes - to add the `staticPredicate` function and to add static files to the list of generators - results in `config.fsx` looking like the following. If we wanted to ignore more folders or files we can add them to the `if` condition.

```fsharp
#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let postPredicate (projectRoot: string, page: string) =
    let fileName = Path.Combine(projectRoot, page)
    let ext = Path.GetExtension page
    if ext = ".md" then
        let ctn = File.ReadAllText fileName
        ctn.Contains("layout: post")
    else
        false

let staticPredicate (projectRoot: string, page: string) =
    let ext = Path.GetExtension page
    if page.Contains ".DS_Store" ||
       page.Contains "_public" ||
       page.Contains ".config" ||
       page.Contains "_lib" ||
       page.Contains ".git" ||
       page.Contains ".ionide" ||
       ext = ".fsx"
    then
        false
    else
        true
 
let config = {
    Generators = [
        {Script = "post.fsx"; Trigger = OnFilePredicate postPredicate; OutputFile = ChangeExtension "html" }
        {Script = "staticfile.fsx"; Trigger = OnFilePredicate staticPredicate; OutputFile = SameFileName }
        {Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
    ]
}
```

Now when we generate the site (run `dotnet fornax watch`) and navigate to the site... we get a page that looks a bit better.

## But the code is ugly...

Just for fun... the site I'm aiming to put up is going to have a lot of code, so code highlighting would be nice.

The solution for this is [highlight.js](https://highlightjs.org/).

At this point I'm not even going to try and embed the source, so there's a gist [post-04.md](https://gist.github.com/recumbent/60391fe57f883bcd0986f4825e9b1acb) - save this into `posts/post-04.md`

Now go to [hightlight.js download](https://highlightjs.org/download/), add F# to the list of styles.

To make life easy, extract it all to a `highlight` folder in the root (you might then want to delete everything except `default.css` from the`highlight/styles` folder).

Then we need to add a the following to the head section `generators/layout.js`

```fsharp
            link [Rel "stylesheet"; Href "/highlight/styles/default.css"]
            script [Src "/highlight/highlight.pack.js"] []
            script [] [!! "hljs.initHighlightingOnLoad();"]   
```

## One more thing

I thought at this point I'd finished... but if you go look at `_public/images/favicon.png` you'll see its not a valid `.png`

This is because we're attempting to treat a binary file as text and, erm, that doesn't end well.

So how do we fix this? Well if in doubt cheat (in a programming context, _sometimes_). In this case there are two bits of cheating

Firstly me - I went to look at the ionide site and found the source that copied binary assets.

Secondly actually solving the problem... add a new file to `loaders` `copyloader.fsx`

```fsharp
#r "../_lib/Fornax.Core.dll"

open System.IO

let loader (projectRoot: string) (siteContent: SiteContents) =
    let intputPath = Path.Combine(projectRoot, "images")
    let outputPath = Path.Combine(projectRoot, "_public", "images")
    if Directory.Exists outputPath then Directory.Delete(outputPath, true)
    Directory.CreateDirectory outputPath |> ignore

    for dirPath in Directory.GetDirectories(intputPath, "*", SearchOption.AllDirectories) do
        Directory.CreateDirectory(dirPath.Replace(intputPath, outputPath)) |> ignore

    for filePath in Directory.GetFiles(intputPath, "*.*", SearchOption.AllDirectories) do
        File.Copy(filePath, filePath.Replace(intputPath, outputPath), true)
    siteContent
```

Then go to config.fsx and add `page.Contains "images" ||` to the list of filters in `staticPredicate`

Now when the site is generated we should end up with a working favicon!

## What next

If you've made it this far, and it all works then I suggest you start again with a clean, empty folder, init git, do the setup, run `dotnet fornax new` and work from there - removing/changing/replacing the pieces as needed to fit your use case.

If it doesn't all work - I'm on twitter [@recumbent](https://twitter.com/recumbent) and will attempt to help (and then to fix up this blog post...)

The finished source will be, erm, somewhere, at some point, maybe (the first iteration of this is as part of an internal blog the source of which I can't publish!).

Murph

