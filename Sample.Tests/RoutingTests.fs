module RoutingTests

open Xunit
open System
open System.Collections.Specialized
open System.Text
open System.Web
open System.Web.Mvc
open System.Web.Routing
open Figment.Helpers
open Figment.Testing

[<Fact>]
let ``get / redirects to /hi`` () =
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
let ``get /hi shows hello world`` () =
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

[<Fact>]
let ``put /hi returns method not allowed``() =
    RouteTable.Routes.Clear()
    SampleApp.Actions.webActions()
    SampleApp.Actions.genericActions()
    let route,controller = getController "PUT" "hi"
    let statusCode = ref 0
    let nv = NameValueCollection()
    let ctx = buildResponse route
                { new HttpResponseBase() with
                    override y.AppendHeader(name, value) =
                        nv.Add(name, value)
                    override y.StatusCode 
                        with get() = !statusCode
                        and set v = statusCode := v }
    ctx.HttpContext <- 
        { new DelegatingHttpContextBase(ctx.HttpContext) with
            override x.Request = (buildRequest "PUT" "hi").Request }
    controller.Execute ctx
    Assert.Equal(405, !statusCode)
    Assert.Equal("GET, HEAD", nv.["Allow"])
    

