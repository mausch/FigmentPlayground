module ExtensionsTests

open System
open Xunit
open WingBeats.Xml
open WingBeats.Xhtml
open Figment

[<Fact>]
let deduplicateScripts() =
    let e = XhtmlElement()
    let s = e.Shortcut
    // http://tweet.seaofclouds.com/

    let twitter username rnd = 
        let divId = sprintf "tweet-%d" rnd
        [
            s.JavascriptFile "http://ajax.googleapis.com/ajax/libs/jquery/1.5/jquery.min.js"
            s.JavascriptFile "jquery.tweet.js"
            e.Div ["id", divId] []
            e.Script [
                &(@"$(function(){
                      $('#"+ divId + @"').tweet({
                        username: '"+ username + @"',
                        avatar_size: 32,
                        count: 4,
                        loading_text: 'searching twitter...'
                      });
                    });")
            ]
        ]
    let page seed = 
        let rnd = Random(seed)
        [
            e.DocTypeHTML5
            e.Html [
                e.Head [
                    e.Title [ &"Tweets!" ]
                ]
                e.Body [
                    e.P [
                        yield! twitter "juliussharpe" (rnd.Next())
                        yield! twitter "sethmacfarlane" (rnd.Next())
                    ]
                ]
            ]
        ]
                 
    //printfn "%s" (Renderer.RenderToString (page Environment.TickCount))
    let pp = deduplicateScriptsForest Set.empty (page Environment.TickCount) |> snd
    printfn "%s" (Renderer.RenderToString pp)
    let page2 = 
        let rec loop (n: Node) c =
            if c > 0
                then loop (e.P [n]) (c-1)
                else n
        loop (e.P [&"hello"]) 2500 // blows up at about 2600
    let pp2 = deduplicateScripts Set.empty page2 |> snd
    //Renderer.RenderToString pp2 |> printfn "%s"
    ()