namespace SampleApp

open Figment.Routing
open Figment.Actions

type PreAppStart() =
    static member Initialize() =
        get "preapp" (content "preapp!")
