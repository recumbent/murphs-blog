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

Previously there was logic in the loader that set a flag to decide if the refresh logic should be included in the layout, we need to get rid of that and just use the shiny new flag...