---
layout: post
title: Improving my fornax blog
author: @recumbent
tags: how-to fornax f#
published: 2020-04-19
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

If I've got this right, then the flaw in my thinking was that I was looking at how to _generate_ the additional pages I wanted not how to _load_ them.

Taking a step back, fornax is built on a two stage process, first you load the content you want to generate and then from that content you generate the pages you need. I was attemping to work out how to generate more pages than I'd loaded whereas in fact what I want to do is load pages that don't acutally exist (and get a bit more creative with file naming).

## Yes, and? Show me the codes...

The thing that I had not yet grokked was that the purpose of the loaders was to stuff things into the `SiteContents` so that the generators can pick from it to create things - we run a generator for everything subject to the filtering provided for the predicates...

So we need to create a new type of content:

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

Next step is to add those into the site content as needed

```fsharp