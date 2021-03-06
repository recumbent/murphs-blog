#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open System
open Html
open Layout

let generateMonth (ctx : SiteContents) (month: Postloader.MonthIndex) =
    let filter year month (date: DateTime) = 
        date.Year = year && date.Month = month

    let posts = 
        ctx.TryGetValues<Postloader.Post> () 
        |> Option.defaultValue Seq.empty
        |> Seq.filter (fun p -> filter month.year month.month p.published)

    let postList =
        posts
        |> Seq.sortByDescending published
        |> Seq.toList
        |> List.map (fun post ->
            li [] [
                makeLink post
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