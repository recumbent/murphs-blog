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
