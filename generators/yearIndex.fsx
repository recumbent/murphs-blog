#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =
    //let published (post: Postloader.Post) =
    //    post.published
    //    |> Option.defaultValue System.DateTime.Now
    //    |> fun n -> n.ToString("yyyy-MM-dd")

    //let year =
    //    ctx.TryGetValues<Postloader.YearIndex> ()
    //    |> Option.defaultValue Seq.empty
    //    |> Seq.find (fun n -> n.file = page)

    //let posts = 
    //    ctx.TryGetValues<Postloader.Post> () 
    //    |> Option.defaultValue Seq.empty
    //    |> Seq.filter (fun p -> (Option.defaultValue DateTime.Minvalue p.published).Year = p.year)

    //let postList =
    //    posts
    //    |> Seq.sortByDescending published
    //    |> Seq.toList
    //    |> List.map (fun post ->
    //        li [] [
    //            a [Href post.link] [!! (sprintf "%s - %s" (published post) post.title)]
    //        ]
    //    )

    let l = Layout.layout ctx "1984" [
        section [] [
            h2 [] [!! "Posts:"]
            ul [] [ li [] [ !! "postList" ] ]
        ]
    ]

    [ ("1984", l) ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> List.map (fun (n,b) -> n, (Layout.render ctx b))