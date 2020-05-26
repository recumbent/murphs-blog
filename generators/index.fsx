#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html
open Layout

let generate' (ctx : SiteContents) (page: string) =
    let posts = ctx.TryGetValues<Postloader.Post> () |> Option.defaultValue Seq.empty
        
    let postList =
        posts
        |> Seq.sortByDescending published
        |> Seq.toList
        |> List.map (fun post ->
            li [] [
                makeLink post
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