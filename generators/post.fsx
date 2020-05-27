#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html


let generate' (ctx : SiteContents) (page: string) =
    let post =
        ctx.TryGetValues<Postloader.Post> ()
        |> Option.defaultValue Seq.empty
        |> Seq.find (fun n -> n.file = page)

    let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo> ()
    let desc =
        siteInfo
        |> Option.map (fun si -> si.description)
        |> Option.defaultValue ""

    let published (post: Postloader.Post) =
        post.published.ToString("yyyy-MM-dd")

    let titleBlock = div [] [
        h1 [Class "titleblock"] [!! post.title]
        span [Class "title-meta"] [!! (sprintf "Author: %s, published: %s, tags: %A" (post.author |> Option.defaultValue "unknown") (published post) post.tags)]
    ]
    
    Layout.layout ctx post.title [
        section [] [
            titleBlock
        ]
        section [] [
            article [] [
                !! post.content
            ]
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx