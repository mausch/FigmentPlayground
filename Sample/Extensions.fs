namespace Figment

open System
open WingBeats.Xml
open System.Web.Mvc

[<AutoOpen>]
module Result =
    let wbview (n: Node list) : Helpers.FAction =
        fun ctx -> Renderer.Render(n, ctx.HttpContext.Response.Output)

    open Formlets

    let formlet (f: _ Formlet) = Result.content (render f)

[<AutoOpen>]
module XhtmlElementExtensions = 
    open WingBeats.Xhtml

    let internal e = XhtmlElement()

    type XhtmlElement with
        member x.DocTypeTransitional = 
            DocType({ name   = "html"
                      pubid  = "-//W3C//DTD XHTML 1.0 Transitional//EN"
                      sysid  = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"
                      subset = null })

        member x.DocTypeHTML5 =
            DocType({ name   = "html"
                      pubid  = null
                      sysid  = null
                      subset = null })
    //<!DOCTYPE html>

    type Shortcuts.XhtmlShortcut with
        member x.Link href text = e.A ["href",href] [&text]

[<AutoOpen>]
module FormletsExtensions =
    open System
    open System.Xml.Linq
    open System.Web.Mvc
    open WingBeats.Xml
    open Figment.Routing
    open Figment.Extensions
    open Formlets
    open Figment.RoutingConstraints

    let runPost formlet (ctx: ControllerContext) =
        let env = EnvDict.fromFormAndFiles ctx.Request
        run formlet env

    let runGet formlet (ctx: ControllerContext) =
        let env = EnvDict.fromNV ctx.QueryString
        run formlet env

    let runParams formlet (ctx: ControllerContext) =
        let env = EnvDict.fromNV ctx.Request.Params
        run formlet env

    type 'a FormActionParameters = {
        Formlet: ControllerContext -> 'a Formlet
        Page: ControllerContext -> XNode list -> Node list
        Success: ControllerContext -> 'a -> Helpers.FAction
    }

    /// <summary>
    /// Maps a page with a formlet and its handler
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="formlet">Formlet to show and process</param>
    /// <param name="page">Function taking an URL and rendered formlet and returning a wingbeats tree</param>
    /// <param name="successHandler">When formlet is successful, run this function</param>
    let formAction url (p: _ FormActionParameters) =
        get url 
            (fun ctx -> 
                let xml = p.Formlet ctx |> renderToXml
                Result.wbview (p.Page ctx xml) ctx)
        post url
            (fun ctx -> 
                match runPost (p.Formlet ctx) ctx with
                | Success v -> p.Success ctx v ctx
                | Failure(errorForm, _) -> Result.wbview (p.Page ctx errorForm) ctx)

    /// <summary>
    /// 'a : state
    /// 'b : form result
    /// 'c : new state
    /// 'd : formlet type
    /// </summary>
    type FormletAction<'a,'b,'c,'d> = ControllerContext -> 'a -> 'b -> ('c * 'd Formlet)

    let internal stateMgr =
        let stateField = "_state"
        let serializer = binSerializer
        let getState (ctx: ControllerContext) =
            ctx.Request.Params.[stateField] |> serializer.Deserialize |> unbox
        let setState v (f: _ Formlet) =
            let v = serializer.Serialize v
            assignedHidden stateField v *> f
        getState,setState
    let internal getState x = fst stateMgr x
    let internal setState x = snd stateMgr x
    let internal copyState (ctx: ControllerContext) =
        getState ctx |> setState
    let internal aform nexturl formlet = form "post" nexturl [] formlet

    let formletToAction nextUrl (f: _ Formlet) (a: FormletAction<_,_,_,_>) : Helpers.FAction =
        fun ctx ->
            let s = getState ctx
            match runParams f ctx with
            | Success v -> 
                let newState, formlet = a ctx s v
                let formlet = setState newState formlet
                let formlet = aform nextUrl formlet
                Result.formlet formlet ctx
            | _ -> failwith "bla"

    let actionFormlet thisFormlet a (url, i) =
        let s i = if i = 0 then "" else i.ToString()
        let thisUrl = url + s i
        let i = i+1
        let nextUrl = url + s i
        action (ifPathIs thisUrl) (formletToAction nextUrl thisFormlet a)
        url,i

    type Web<'a> = ControllerContext -> 'a Formlet


    open System.Reflection

    type WebBuilder() =
        let makeCont (f: 'a -> Web<'b>) (formlet: 'a Formlet) : Web<'b> =
            fun ctx ->
                match runParams formlet ctx with
                | Success v -> f v ctx
                | _ -> failwith "booooo"

        let aform2 formlet = form "post" "" [] formlet
        
        member x.Bind(a: Web<'a>, f: 'a -> Web<'b>): Web<'a> = 
            fun (ctx: ControllerContext) ->
                let formlet = a ctx
                let cont = box (makeCont f formlet), box typeof<'b>
                formlet |> setState cont |> aform2

        member x.Return a : Web<_> = 
            fun ctx ->
                failwith "return"

        member x.ReturnFrom a = a

        member x.ShowFormlet (formlet: 'a Formlet) : Web<_> =
            fun ctx -> formlet

        member x.ToAction (w: Web<_>) : Helpers.FAction =
            fun ctx ->            
                let cont = getState ctx
                match cont with
                | null -> 
                    Result.formlet (w ctx) ctx
                | _ -> 
                    let t: Type = cont.GetType().GetProperty("Item2").GetValue(cont, null) |> unbox
                    let c = cont.GetType().GetProperty("Item1").GetValue(cont, null)
                    let formlet = c.GetType().GetMethod("Invoke").Invoke(c, [|ctx|])
                    let figmentResultType = Type.GetType("Figment.Result, Sample")
                    let rftm = figmentResultType.GetMethod("formlet", BindingFlags.Static ||| BindingFlags.Public)
                    let rftmg = rftm.GetGenericMethodDefinition()
                    let rf = rftmg.MakeGenericMethod([|t|])                
                    let r = rf.Invoke(null, [|formlet|])
                    unbox r


[<AutoOpen>]
module ConnegIntegration =
    open FsConneg
    open Figment.Extensions
    open Figment.RoutingConstraints
    open Figment.Result
    open Figment.ReaderOperators

    let internal haccept = "Accept"

    let internal accepted (ctx: ControllerContext) =
        ctx.Request.Headers.[haccept]

    let filterMediaTypes media (ctx: ControllerContext) =
        FsConneg.filterMediaTypes media (accepted ctx)

    let negotiateMediaType media (ctx: ControllerContext) =
        FsConneg.negotiateMediaType media (accepted ctx)

    let bestMediaType media (ctx: ControllerContext) =
        FsConneg.bestMediaType media (accepted ctx)

    let bestLanguage lang (ctx: ControllerContext) =
        FsConneg.bestLanguage lang (accepted ctx)

    module Result =
        let notAcceptable x = status 406 x
        let methodNotAllowed allowedMethods = 
            status 405 >>. allow allowedMethods

    /// <summary>
    /// Routing function that matches if client accepts any of the specified media types
    /// </summary>
    /// <param name="media"></param>
    let ifAcceptsAny media : RouteConstraint =
        fun (ctx, route) ->
            let acceptable = FsConneg.negotiateMediaType media ctx.Request.Headers.[haccept]
            acceptable.Length > 0

    /// <summary>
    /// Negotiates response media type
    /// </summary>
    /// <param name="writers">Table of available media type writers</param>
    /// <param name="action"></param>
    let negotiateActionMediaType writers action =
        let servedMedia = List.collect fst writers
        let bestOf = accepted >> FsConneg.bestMediaType servedMedia >> Option.map fst
        let findWriterFor mediaType = List.find (fst >> List.exists ((=)mediaType)) >> snd
        fun ctx ->
            let a = 
                match bestOf ctx with
                | Some mediaType -> 
                    let writer = writers |> findWriterFor mediaType
                    action >>= writer >>. vary "Accept"
                | _ -> Result.notAcceptable
            a ctx

[<AutoOpen>]
module WingBeatsExtensions =
    let internal isSrc = fst >> (fun n -> n.Name = "src")
    let internal tryGetSrc attr = attr |> List.tryFind isSrc |> Option.map snd

    let rec procNode tagPairNodeF state =
        function
        | TagPairNode(name, attr, children) ->
            let state, children = procForest tagPairNodeF state children
            let node = TagPairNode(name, attr, children)
            tagPairNodeF state name attr children node
        | x -> state,x
    and procForest tagPairNodeF state nodes =
        let folder (state,nodes) n =
            let state, node = procNode tagPairNodeF state n
            state, node::nodes
        let state, nodes = Seq.fold folder (state,[]) nodes
        state, List.rev nodes

    let dedup state name attr children node =
        if name.Name <> "script"
            then state, node
            else 
                match tryGetSrc attr with
                | Some src -> 
                    if Set.contains src state
                        then state, NoNode
                        else 
                            let state = Set.add src state
                            state, node
                | _ -> state, node

    let deduplicateScripts = procNode dedup
    let deduplicateScriptsForest x = procForest dedup x
