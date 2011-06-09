namespace SampleApp

open System
open System.Web

type MvcApplication() =
    inherit HttpApplication()
    member this.Application_Start() = 
        Actions.webactions()
        Actions.notfound()