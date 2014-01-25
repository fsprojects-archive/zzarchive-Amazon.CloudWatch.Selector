type unop =
    | Neg

type binop =
    | Add
    | Sub
    | Mul
    | Div
    | Pow

let binOpOfChar = function
    | '+' -> Add
    | '-' -> Sub
    | '*' -> Mul
    | '/' -> Div
    | '^' -> Pow
    | c -> invalidArg "binOpOfChar" (string c);;

type expr =
    | Int of int
    | UnOp of unop * expr
    | BinOp of binop * expr * expr
    static member (~-) f = UnOp(Neg, f)
    static member (+) (f, g) = BinOp(Add, f, g)
    static member (-) (f, g) = BinOp(Sub, f, g)
    static member (*) (f, g) = BinOp(Mul, f, g)
    static member (/) (f, g) = BinOp(Div, f, g)
    static member Pow(f, g) = BinOp(Pow, f, g)

let apply exprs op =
    match exprs with
    | arg2::arg1::exprs -> BinOp(op, arg1, arg2)::exprs
    | _ -> invalidArg "parse" "Mismatched parenthesis."

let rec op (ops, opss) exprs op1 =
    match op1, ops with
    | (Add | Sub), (Add | Sub | Mul | Div | Pow as op2)::ops
    | (Mul | Div), (Mul | Div | Pow as op2)::ops ->
        op (ops, opss) (apply exprs op2) op1
    | op1, ops -> ((op1::ops), opss), exprs

let rec aux ((ops, opss), exprs) token =
    match (ops, opss), token with
    | opss, ('+' | '-' | '*' | '/' | '^' as c) -> op opss exprs (binOpOfChar c)
    | (ops, opss), '(' -> ([], ops::opss), exprs
    | (op::ops, opss), ')' -> aux ((ops, opss), apply exprs op) token
    | ([], ops::opss), ')' -> (ops, opss), exprs
    | (_, []), ')' -> invalidArg "parse" "Mismatched parenthesis."
    | qss, c -> qss, Int(int(string c))::exprs

let parse tokens =
    let (ops, opss), exprs = Seq.fold aux (([], []), []) tokens
    match Seq.fold apply exprs (Seq.append [ops] opss |> Seq.concat) with
    | [expr] -> expr
    | _ -> invalidArg "parse" "Syntax error"

let rec (|Expr|) = function
    | P(f, xs) -> Expr(loop (' ', f, xs))
    | xs -> invalidArg "Expr" (sprintf "%A" xs)
and loop = function
    | ' ' as oop, f, ('+' | '-' as op)::P(g, xs)
    | (' ' | '+' | '-' as oop), f, ('*' | '/' as op)::P(g, xs)
    | oop, f, ('^' as op)::P(g, xs) ->
        let h, xs = loop (op, g, xs)
        loop (oop, BinOp(binOpOfChar op, f, h), xs)
    | _, f, xs -> f, xs
and (|P|_|) = function
    | '('::Expr(f, ')'::xs) -> Some(P(f, xs))
    | c::_ as xs when '0' <= c && c <= '9' ->
        let rec loop n = function
            | c2::xs when '0' <= c2 && c2 <= '9' -> loop (10*n + int(string c2)) xs
            | xs -> Some(P(Int n, xs))
        loop 0 xs
    | _ -> None