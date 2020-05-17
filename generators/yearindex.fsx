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
    
    let content = Layout.layout ctx title  [
        section [] [
            h2 [] [!! (sprintf "Posts for %s" title)]
            ul [] postList
        ]
    ]

    title, content

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