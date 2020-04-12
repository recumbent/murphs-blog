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