---
layout: post
title: Changing the folder structure of the blog
author: @recumbent
tags:
---

One of the things I want is for the posts and the folder structure to be inherently discoverable. I'm some way there - but not quite where I wanted and in looking at what I've already done its clear to me that the `posts` folder is redundant. Similarly if I'm doing this "right" I do need to have a folder for each date (for all that I don't expect to publish multiple posts on the same day)

Its kinda important to get these changes out of the way as early as possible - before I do anything that might result in google taking notice - because whilst its no exactly difficult to set up redirects, I'd rather not (yet).

As I'm on playing with the structure its probably an opportunity to fix up some other pages - tags for example - without too much effort. After that I'm afraid I need to give myself a to do list (erm, "backlog"?) of things that might benefit from improvement.

## But first...

The _very_ first thing I need to do is to enable live update in watch mode again - this was causing me issues for various reasons but since my first pass at the blog Fornax has been updated and now will flag if its in watch mode.   

Specifically the code that calls the generators now has this line:

```
    if isWatch then  yield "--define:WATCH"`
```

Previously there was logic in the loader that set a flag to decide if the refresh logic should be included in the layout, we need to get rid of that and just use the shiny new flag, that changes the `render` function in `generators/layout.fsx` to

```fsharp
let render (ctx : SiteContents) content =
  content
  |> HtmlElement.ToString
#if WATCH
  |> injectWebsocketCode
#endif
```

And attempting to test that tells me that my shiny new use of a drafts folder _breaks_ the generator. So now we have to fix _that_ before we can contine.

### Fixing the build issue

There are two possibilities here, on the one hand I don't really want the drafts folder to be included when I build (for publication) but on the other it would be nice to be able to preview the drafts when I'm working on them.

We can address the first problem fairly directly by excluding the drafts folder when we're not in watch mode.

If we change the in `config.fsx` to the following (which is a bit clunky):

```fsharp
let postPredicate (projectRoot: string, page: string) =
    let fileName = Path.Combine(projectRoot, page)
    let ext = Path.GetExtension page
#if !WATCH
    if not (page.Contains "drafts") && ext = ".md" then
#else 
    if ext = ".md" then
#endif
        let ctn = File.ReadAllText fileName
        ctn.Contains("layout: post")
    else
        false
```

Then if we're not in watch mode we _won't_ treat the drafts folder as posts and all will be good.

That also means we need to make an addition to the list of filters for static content:

```fsharp
        page.Contains "drafts" ||
```

without this build will make a literal copy of any draft posts (which will then get published)

### _Not_ fixing the watch issue

At this point I got lost - actually I had a lot of fun exploring the possibilities of de-dupication and other things but in so doing I ended up in a completely broken state (to do this I need to do that which leads to something else). It turns out that I don't know enough about how F# scripts behave yet, and I suspect that my development process could be better - that in particular I'm not making enough use of the REPL to test my code. That's definitely a story I need to explorer further.

Instead I took a bit of guidance from the [Mikado method](https://pragprog.com/magazines/2010-06/the-mikado-method) and threw away (well branched away) my work and did a hard reset.

I'm going to park the watch issue for bit to continue with my original goal of fixing the folder structure.

## Fixing the folder structure

### 1. There has to be a date

First decision is to make the published date on a post mandatory - if there's no published date in the front matter (there won't be if its a draft) use "Today". I can do this because its my bat and my ball and that rule fits my needs. Published in a post becomes:

```fsharp
    published: System.DateTime
```

That triggers a whole load of things that need to be fixed, which in turn makes them less complicated

When creating the post to be stored in the content we need to map from an option

```fsharp
      published = published |> Option.defaultValue DateTime.Today
```

We no longer have to worry about the optin in `processPost` giving:

```fsharp
let processPost (siteContent: SiteContents) (post: Post) =
    siteContent.Add post
    processYearIndex siteContent post.published
    processMonthIndex siteContent post.published
```

I can also remove `siteContent.Add({disableLiveRefresh = true})` from the `loader` function as we don't work that way any more.

### 2. Everything is broken

Turns out that published - as an option - is referenced a lot...

I have this function in several places:

```fsharp
let published (post: Postloader.Post) =
    post.published
    |> Option.defaultValue System.DateTime.MinValue
    |> fun n -> n.ToString("yyyy-MM-dd")
```

We want to reduce the number of places but as published is no longer an option we can simplify this to the following wherever we find it:

```fsharp
let published (post: Postloader.Post) =
    post.published.ToString("yyyy-MM-dd")
```

And similarly everywhere else we reference published, which allows build to work again

### 3. De duplication of links to posts

I have something like the following in at least 3 places in the code:

```fsharp
    a [Href post.link] [!! (sprintf "%s - %s" (published post) post.title)]
```

There's scope there for a bit of de-duplication, first we push `published` into layout, then we add a new function in layout

```fsharp
let makeTitle (post : Postloader.Post) =
    sprintf "%s - %s" (published post) post.title
```

For now we'll keep the `Href` pointing to `post.link` (although that's going to be wrong - I need to fix that) but to be consistent lets wrap that in a function too:

```fsharp
let makePath (post: Postloader.Post) = 
    post.link
```

And finally we can have a function for the whole `<a>...</a>`:

```fsharp
let makeLink (post: Postloader.Post) = 
      a [Href (makePath post)] [!! (makeTitle post)]
```

Which leaves me using

```fsharp
    makeLink post
```

And with that able to change the way I put the title together in a single location. I want to do something better with tags (and the author) but that's for another day.

### 3. Links are broken now

I've moved the files around, but I haven't changed the link creation logic. The logic for link generation is in `postloader.fsx` and also to some extent in the generators `yearindex.fsx` and `monthindex.fsx` - not sure if I can tidy this all up.

The particular challenge is that we create the path in two different contexts, one is purely from the filename in `config.fsx` and the other is for links as part of generation, that said we've addressed the first part so we'll not worry too much.

Remove link from the model in `postloader.fsx` as it is no longer needed.

Change the `makePath` method in `layout.fsx` to:

```fsharp
let makePath (post: Postloader.Post) = 
    sprintf "/%04i/$%02i/%02i/%s.html" post.published.Year post.published.Month post.published.Day post.title
```

### 3. Watch is _still_ broken

My problem with watch is that I can't find drafts when attempting to render them as a post.

To fix this I'm going to remove the extras from the value stored for "file" which in turn is the key used to find the data during the generation phase.