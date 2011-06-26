module RoutingTests

open Xunit
open System
open System.Text
open System.Web
open System.Web.Mvc
open System.Web.Routing

let buildReq verb path =
    { new HttpContextBase() with
        override x.Request = 
            { new HttpRequestBase() with
                override y.HttpMethod = verb
                override y.RawUrl = path
                override y.PathInfo = ""
                override y.AppRelativeCurrentExecutionFilePath = "~/"
                override y.Url = Uri("http://localhost/" + path) }}

let getController verb path =
    let route = buildReq verb path |> RouteTable.Routes.GetRouteData
    Assert.NotNull route
    let handler : Figment.IControllerProvider = unbox <| route.RouteHandler.GetHttpHandler(RequestContext())
    Assert.NotNull handler
    route, handler.CreateController()

let buildResp route resp =
    let ctx =
        { new HttpContextBase() with
            override x.Session = 
                { new HttpSessionStateBase() with
                    override y.Item 
                        with get (k:string) = box null 
                        and set (k: string) (v:obj) = () }
            override x.Request =
                { new HttpRequestBase() with
                    override y.ValidateInput() = ()
                    override y.Path = "" }
            override x.Response = resp }
    RequestContext(ctx, route)

[<Fact>]
let ``get /`` () =
    RouteTable.Routes.Clear()
    SampleApp.Actions.webActions()
    let route,controller = getController "GET" ""
    let redirectUrl = ref ""
    let ctx = buildResp route
                { new HttpResponseBase() with
                    override y.Redirect(url, e) = redirectUrl := url; () }
    controller.Execute ctx
    Assert.Equal("hi", !redirectUrl)

[<Fact>]
let ``get /hi`` () =
    RouteTable.Routes.Clear()
    SampleApp.Actions.webActions()
    let route,controller = getController "GET" "hi"
    let sb = StringBuilder()
    let ctx = buildResp route
                { new HttpResponseBase() with
                    override y.Write(s: string) = 
                        sb.Append s |> ignore }
    controller.Execute ctx
    Assert.Contains("Hello World", sb.ToString())