namespace SampleApp

open System
open System.Web
open SampleApp.Actions

[<assembly: PreApplicationStartMethod(typeof<PreAppStart>, "Initialize")>]
do()

type MvcApplication() =
    inherit HttpApplication()
    member this.Application_Start() = 
        webactions()
        notfound()