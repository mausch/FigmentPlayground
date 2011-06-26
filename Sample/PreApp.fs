namespace SampleApp

open Figment.Routing
open Figment.Result

type PreAppStart() =
    static member Initialize() =
        get "preapp" (content "preapp!")
