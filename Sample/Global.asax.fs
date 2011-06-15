namespace SampleApp

open System
open System.Web
open System.Web.Mvc
open SampleApp.Actions

// demo registering some actions via PreApplicationStartMethodAttribute
[<assembly: PreApplicationStartMethod(typeof<PreAppStart>, "Initialize")>]
do()

type MvcApplication() =
    inherit HttpApplication()
    member this.Application_Start() = 
        // standard ASP.NET MVC filter demo
        let filter = 
            { new IActionFilter with
                member x.OnActionExecuted ctx = ()
                member x.OnActionExecuting ctx = 
                    ctx.HttpContext.Response.AppendHeader("X-Figment-Version", "0.01") }

        GlobalFilters.Filters.Add filter

        // register Figment actions
        webActions()
        genericActions()