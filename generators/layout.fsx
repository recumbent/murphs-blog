#r "../_lib/Fornax.Core.dll"

open Html

// Layout helpers

let published (post: Postloader.Post) =
    post.published.ToString("yyyy-MM-dd")

let makePath (post: Postloader.Post) = 
    sprintf "/%04i/$%02i/%02i/%s.html" post.published.Year post.published.Month post.published.Day post.title
    
let makeTitle (post : Postloader.Post) =
    sprintf "%s - %s" (published post) post.title

let makeLink (post: Postloader.Post) = 
      a [Href (makePath post)] [!! (makeTitle post)]

// Layout common

let injectWebsocketCode (webpage:string) =
    let websocketScript =
        """
        <script type="text/javascript">
          var wsUri = "ws://127.0.0.1:8080/websocket";
      function init()
      {
        websocket = new WebSocket(wsUri);
        websocket.onclose = function(evt) { onClose(evt) };
      }
      function onClose(evt)
      {
        console.log('closing');
        websocket.close();
        document.location.reload();
      }
      window.addEventListener("load", init, false);
      </script>
        """
    let head = "<head>"
    let index = webpage.IndexOf head
    webpage.Insert ( (index + head.Length + 1),websocketScript)

let layout (ctx : SiteContents) active bodyContent =
    html [] [
        head [] [
            meta [CharSet "utf-8"]
            title [] [!! "Murph's Blog"]
            link [Rel "icon"; Type "image/png"; Sizes "32x32"; Href "/images/favicon.png"]
            link [Rel "stylesheet"; Type "text/css"; Href "/css/styles.css"]
            link [Rel "stylesheet"; Href "/highlight/styles/darcula.css"]
            script [Src "/highlight/highlight.pack.js"] []
            script [] [!! "hljs.initHighlightingOnLoad();"] 
        ]
        body [] [
            header [] [
              section [Class "hero"] [
                div [Class "content hero-title"] [!! "Murph's random witterings"]
              ]
              nav [Class "content"] [
                a [Href "/"][!! "Home"]
                span [] [!! " | "]
                a [Href "/posts"][!! "Archive"]
                span [] [!! " | "]
                a [Href "/tags"][!! "Tags"]
                span [] [!! " | "]
                a [Href "/about.html"][!! "About"]
              ]
            ]
            section [Class "content"] [
              yield! bodyContent
            ]
            footer [Class "content"] [
              hr []
              p [] [!! (sprintf "Copyright 2020-%i James Murphy" System.DateTime.Today.Year)]              
            ]
        ]
    ]

let render (ctx : SiteContents) content =
  content
  |> HtmlElement.ToString
#if WATCH
  |> injectWebsocketCode
#endif
   