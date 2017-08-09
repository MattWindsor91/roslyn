module FsTypeClasses.Main 

  //let inline N<'a> = Unchecked.defaultof<'a>
  // Haskell type class
  type Num<'A> =
      abstract Add: 'A * 'A ->'A
      abstract Mult: 'A * 'A ->'A
      abstract Neg: 'A -> 'A

  type C() = 
    static member Foo = (0,0)


  // Haskell type class instance for int
  [<Struct>]
  type NumInt =
       interface Num<int> with
         member this.Add(a,b) = a + b
         member this.Mult(a,b) = a + b
         member this.Neg a = -a
       end

   // Haskell  type class instance for float
  [<Struct>]
  type NumFloat =
       interface Num<float> with
         member this.Add(a,b) = a + b
         member this.Mult(a,b) = a + b
         member this.Neg a = -a
       end
  
  [<Struct>]
  type NumPair<'A,'B,'NumA,'NumB when 'NumA : struct and 'NumA:> Num<'A> 
                                 and  'NumB : struct and 'NumB:> Num<'B> 
                                      >  =
       interface Num<'A*'B> with
         member this.Add((a1,a2),(b1,b2)) = (Unchecked.defaultof<'NumA>.Add(a1,b1) ,  Unchecked.defaultof<'NumB>.Add(a2,b2))
         member this.Mult((a1,a2),(b1,b2))  = (Unchecked.defaultof<'NumA>.Add(a1,b1) ,  Unchecked.defaultof<'NumB>.Add(a2,b2))
         member this.Neg ((a1,a2)) = (Unchecked.defaultof<'NumA>.Neg(a1) ,  Unchecked.defaultof<'NumB>.Neg(a2))
       end
 


  // Haskell operator overloading
  //add: forall 'a,Num 'a => 'a * 'a -> 'a
  let add<'A,'N when 'N : struct  and 'N :> Num<'A>>(a,b) =
      let N = Unchecked.defaultof<'N>
      N.Add(a,b)
      
  type term<'A> = Val of 'A
                | Plus of term<'A> * term<'A>
                | Mult of term<'A> * term<'A>
                | Neg of term<'A>

  let eval<'A,'N when 'N:struct and 'N:>Num<'A>> t= 
      // (stack) allocate the dictionary for 'N
      let N = Unchecked.defaultof<'N> 
      let rec eval t = 
           match t with 
           | Val v -> v
           | Plus (a, b) -> N.Add(eval a,eval b) 
           | Mult (a, b) -> N.Mult(eval a,eval b)
           | Neg a -> N.Neg(eval a)
      in eval t

  let 3 = eval<_,NumInt> (Plus (Val 1 , Val 2))
  let 3.0 = eval<_,NumFloat> (Plus (Val 1.0 , Val 2.0))
  let (3,3.0) = eval<_,NumPair<_,_,NumInt,NumFloat>> (Plus (Val (1,1.0), Val(2,2.0)))
  let _ = System.Console.ReadLine()




