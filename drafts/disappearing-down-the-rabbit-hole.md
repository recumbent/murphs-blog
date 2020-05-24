---
layout: post
title: Disappearing down the rabbit hole
author: @recumbent
tags: opinion, rabbit holes
---

The best and the worst of being a programmer* is the potential for getting distracted. In personal projects its mostly an additional source of entertainment - we do personal projects to learn and explore. In a work context (or where there are more concrete goals) it can be more of a challenge - one I don't have a good answer to.

Its worth noting that in an agile context the rabbit holes may well be explorations of the unknowns we have to resolve - its why one should address the _hard_ problems, the problems we don't know how alreadt know how to solve, first.

## What do rabbit holes look like

I'm a programmer, so for me the solution to _every_ problem is code (its not, but that's a different conversation).

One set of problems is automating things that we do frequently, the rabbit hole that follows this nicely described by [XKCD](https://xkcd.com/974/):

![The General Problem - XKCD](https://imgs.xkcd.com/comics/the_general_problem.png)

And in chasing the general solution we often fail to deliver something specific and immediate.

The other one, the more insidious one, (and one that often arises as we generalise problems as above) is [yak shaving](http://projects.csail.mit.edu/gsb/old-archive/gsb-archive/gsb2000-02-11.html) this is the most fun to explore as it results from taking a ride on what my son likes to describe as the "bus of thought" (see below, explaining here is...)

### An example

A really brief example from working on this blog - I want to put a sensible date on my published posts, this means I _don't know_ what the "published" date will be when I start writing. Also I want to get the header block right (consistent).

But writing a helper (`new-post.fsx`) means I'm not writing a blog post, or improving the appearence or... and then having written the first helper I now need to write the second (`publish-post.fsx` which doesn't exist as I type this, but might soon) to move and rename the file and to inject the date.

### An more extensive journey

That doesn't really explore the proper depths of the problem though...

* In [automating deployment using github actions](https://blog.murph.me.uk/2020/05/23/deploying-via-github-actions.html) I found that I was running a complex docker build step to do a file copy (to blob storage) 
* So could I automate that using an F# script...
* Oh wait - the cache for the home page hasn't invalidated
* How do I invalidate an [Azure CDN](https://azure.microsoft.com/en-us/services/cdn/) endpoint cache? - [Purge](https://docs.microsoft.com/en-us/azure/cdn/cdn-purge-endpoint) - ok so can I do that from my action?
* How do I authenticate to Azure to call "purge" 
* What's a [service principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
* Can I create one using resource templates... ooh [Farmer](https://compositionalit.github.io/farmer/)... - possibly not the best plan (that branch looks very deep, and quite dark)
* So authentication... ok OAuth... rest... hmmm... sdks... 
* But that means I need packages and nuget kind of expects a project
* But I'm doing F# so [Paket](https://fsprojects.github.io/Paket/index.html) specifically [Using Paket from F# Interactive](https://fsprojects.github.io/Paket/reference-from-repl.html)
* Ah... if I've got a service principal _and_ I can install the necessary bits of the azure sdks then we're back to can I use .fsx to do the file copy etc...
* Sideways - can I get the ARM templates for deploying the blog, because, reasons...

So now I've got a _lot_ of tabs open on multiple computers - and this is just the core of my thinking, there are elements of "how do I track what I need to do" which could launch me into yet more yak shaving! (I have a generalised wish to use something like [todo.txt](https://github.com/todotxt/todo.txt) and half a powershell solution - but perhaps the tooling in github or maybe something else...)

And in all of that what have I achieved? Worse, almost all of the above are things I probably want to learn anyway!

Well... I have created a new post helper. And having written _this_ post I'm going to write a publish post helper. Then I think that cache invalidation thing is going to annoy me (and yes, I know the right answer is to redeploy to static web apps, but that's a bigger yak!)

Murph

## The Bus of Thought

We have an expression "lost my train of thought" - but it was observed that we percieve trains as things that head fairly directly from point A to point B at useful speeds whereas our thoughts meander all over the place visiting odd corners and taking way too long.

In the UK over the past few decades lack of funding for public transport has resulted in bits of routes being mashed together so that buses take all kinds of interesting (and time consuming) little diversions along the way from A to B.

It doesn't really matter how much one is stretching the analogy ("pinball of thought" may be better still) we kinda like it.