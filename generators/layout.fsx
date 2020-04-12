#r "../_lib/Fornax.Core.dll"

open Html

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
              h1 [] [!! "Murph's Blog!"]
              a [Href "/"][!! "Home"]
            ] 
            yield! bodyContent
        ]
    ]

let render (ctx : SiteContents) content =
  let disableLiveRefresh = ctx.TryGetValue<Postloader.PostConfig> () |> Option.map (fun n -> n.disableLiveRefresh) |> Option.defaultValue false
  content
  |> HtmlElement.ToString
  |> fun n -> if disableLiveRefresh then n else injectWebsocketCode n
  