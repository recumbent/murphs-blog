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
        post.published
        |> Option.defaultValue System.DateTime.Now
        |> fun n -> n.ToString("yyyy-MM-dd")

    Layout.layout ctx post.title [
        section [] [
            div [] [
                div [] [
                    h1 [] [!!desc]
                ]
            ]
        ]
        div [] [
            section [] [
                div [] [
                    div [] [
                        div [] [
                            div [] [
                                p [] [ a [Href post.link] [!! post.title]]
                                p [] [
                                a [Href "#"] [!! (defaultArg post.author "")]
                                !! (sprintf "on %s" (published post))
                                ]
                            ]
                            article [] [
                                !! post.content
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx