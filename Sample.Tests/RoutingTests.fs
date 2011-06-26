module RoutingTests

open Xunit
open System
open System.Text
open System.Web
open System.Web.Mvc
open System.Web.Routing
open Figment.Testing

[<Fact>]
let ``get /`` () =
    RouteTable.Routes.Clear()
    SampleApp.Actions.webActions()
    let route,controller = getController "GET" ""
    let redirectUrl = ref ""
    let ctx = buildResponse route
                { new HttpResponseBase() with
                    override y.Redirect(url, e) = 
                        redirectUrl := url }
    controller.Execute ctx
    Assert.Equal("hi", !redirectUrl)

[<Fact>]
let ``get /hi`` () =
    RouteTable.Routes.Clear()
    SampleApp.Actions.webActions()
    let route,controller = getController "GET" "hi"
    let sb = StringBuilder()
    let ctx = buildResponse route
                { new HttpResponseBase() with
                    override y.Write(s: string) = 
                        sb.Append s |> ignore }
    controller.Execute ctx
    Assert.Contains("Hello World", sb.ToString())