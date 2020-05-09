---
layout: post
title: Better binary file handling in my fornax blog
author: @recumbent
tags: how-to
published: 2020-05-03
---

I built my blog - and wrote my first post - using [Fornx](https://github.com/ionide/fornax) v0.11.0 and had to get a bit creative to copy binary files (the favicon).

This was addressed in [0.12.0](https://github.com/ionide/Fornax/releases/tag/0.12.0) by allowing the core code to output a byte array instead of lines of text (the changes are in this commit: [Allow generate to return a byte array](https://github.com/ionide/Fornax/commit/b4a575a651ce75e7805834de40263f457e3b7f4c#diff-7cb9fd4a13259bde5ff5e815d2456368L310)).

So how does one take advantage of this?

## Updating fornax

First thing to do is to update the version of fornax the project uses, I'm using a local installation, so I want to run the following:

```
dotnet tool update --local fornax
```
That done you can see what version is installed using `dotnet fornax version` (at time of typing 0.13.1)

The next thing is to copy the .dll's to the bin folder. Because nuget packages are cached globally you'll need to go find them - these are in a folder underneath your user:

* Windows `%userprofile%\.nuget\packages`
* Mac/Linux: `~/.nuget/packages`

So on my machine, for this version, I want to copy `C:\Users\james\.nuget\packages\fornax\0.13.1\tools\netcoreapp3.1\any\Fornax.Core.dll` and `C:\Users\james\.nuget\packages\fornax\0.13.1\Fornax.Template\_lib\Markdig.dll` to the `_lib` folder in the root of my blog.

n.b. in F# 5 scripts will be able to reference packages directly from nuget as follows: `#r "nuget: Newtonsoft.Json"` at which point the _lib folder will be redundant.

At this point I would do a clean and a build or a watch to make sure everything still works as expected and then commit the changes...

## Taking out the loader hack

My current implementation abuses a loader to copy binary files, instead we can now make the behaviour more consistent with the load/generate pattern as binary files just become static files.

Start by deleting `copyloader.fsx` from `loaders` as its no longer required.

Next change `generators\staticfile.fsx` to look like this:

```fsharp
#r "../_lib/Fornax.Core.dll"

open System.IO

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    let inputPath = Path.Combine(projectRoot, page)
    File.ReadAllBytes inputPath
```

All we've done here is change `File.ReadAllLines` to `File.ReadAllBytes` but this is the critical fix.

Finally we remove the binary file filters (`ext = ".png" || ext = ".ico"`) from the `staticPredicate` in `config.fsx` so it now looks like:

```fsharp
let staticPredicate (projectRoot: string, page: string) =
    let ext = Path.GetExtension page
    not (
        page.Contains ".DS_Store" ||
        page.Contains "_public" ||
        page.Contains ".config" ||
        page.Contains "_lib" ||
        page.Contains ".git" ||
        page.Contains ".ionide" ||
        page.Contains ".vs" ||
        ext = ".fsx"
    )
```

A clean and a build should now copy binary files _intact_ to the generated output without having to "cheat" by copying them in the loader. 

Concerns separated again - much nicer.

Murph

