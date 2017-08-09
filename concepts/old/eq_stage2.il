
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly eq_stage2
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module eq_stage2.exe
// MVID: {D26E9D95-9802-4708-BAEE-6EC3D259D33B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x07290000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit ConceptAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ConceptAttribute::.ctor

} // end of class ConceptAttribute

.class private auto ansi beforefieldinit ConceptInstanceAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ConceptInstanceAttribute::.ctor

} // end of class ConceptInstanceAttribute

.class interface private abstract auto ansi Eq`1<A>
{
  .custom instance void ConceptAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance bool  Equals(!A a,
                                !A b) cil managed
  {
  } // end of method Eq`1::Equals

} // end of class Eq`1

.class private sequential ansi sealed beforefieldinit EqInt
       extends [mscorlib]System.ValueType
       implements class Eq`1<int32>
{
  .pack 0
  .size 1
  .custom instance void ConceptInstanceAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot virtual final 
          instance bool  Equals(int32 a,
                                int32 b) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldarg.2
    IL_0002:  ceq
    IL_0004:  ret
  } // end of method EqInt::Equals

} // end of class EqInt

.class private sequential ansi sealed beforefieldinit EqArray`2<A,valuetype .ctor (class Eq`1<!A>, [mscorlib]System.ValueType) EqA>
       extends [mscorlib]System.ValueType
       implements class Eq`1<!A[]>
{
  .pack 0
  .size 1
  .custom instance void ConceptInstanceAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot virtual final 
          instance bool  Equals(!A[] a,
                                !A[] b) cil managed
  {
    // Code size       131 (0x83)
    .maxstack  4
    .locals init (!EqA V_0,
             bool V_1,
             bool V_2,
             bool V_3,
             bool V_4,
             int32 V_5,
             bool V_6,
             bool V_7)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    !EqA
    IL_0009:  ldarg.1
    IL_000a:  ldnull
    IL_000b:  ceq
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  brfalse.s  IL_0018

    IL_0011:  ldarg.2
    IL_0012:  ldnull
    IL_0013:  ceq
    IL_0015:  stloc.2
    IL_0016:  br.s       IL_0081

    IL_0018:  ldarg.2
    IL_0019:  ldnull
    IL_001a:  ceq
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brfalse.s  IL_0024

    IL_0020:  ldc.i4.0
    IL_0021:  stloc.2
    IL_0022:  br.s       IL_0081

    IL_0024:  ldarg.1
    IL_0025:  ldlen
    IL_0026:  conv.i4
    IL_0027:  ldarg.2
    IL_0028:  ldlen
    IL_0029:  conv.i4
    IL_002a:  ceq
    IL_002c:  ldc.i4.0
    IL_002d:  ceq
    IL_002f:  stloc.s    V_4
    IL_0031:  ldloc.s    V_4
    IL_0033:  brfalse.s  IL_0039

    IL_0035:  ldc.i4.0
    IL_0036:  stloc.2
    IL_0037:  br.s       IL_0081

    IL_0039:  ldc.i4.0
    IL_003a:  stloc.s    V_5
    IL_003c:  br.s       IL_0070

    IL_003e:  nop
    IL_003f:  ldloca.s   V_0
    IL_0041:  ldarg.1
    IL_0042:  ldloc.s    V_5
    IL_0044:  ldelem     !A
    IL_0049:  ldarg.2
    IL_004a:  ldloc.s    V_5
    IL_004c:  ldelem     !A
    IL_0051:  constrained. !EqA
    IL_0057:  callvirt   instance bool class Eq`1<!A>::Equals(!0,
                                                              !0)
    IL_005c:  ldc.i4.0
    IL_005d:  ceq
    IL_005f:  stloc.s    V_6
    IL_0061:  ldloc.s    V_6
    IL_0063:  brfalse.s  IL_0069

    IL_0065:  ldc.i4.0
    IL_0066:  stloc.2
    IL_0067:  br.s       IL_0081

    IL_0069:  nop
    IL_006a:  ldloc.s    V_5
    IL_006c:  ldc.i4.1
    IL_006d:  add
    IL_006e:  stloc.s    V_5
    IL_0070:  ldloc.s    V_5
    IL_0072:  ldarg.1
    IL_0073:  ldlen
    IL_0074:  conv.i4
    IL_0075:  clt
    IL_0077:  stloc.s    V_7
    IL_0079:  ldloc.s    V_7
    IL_007b:  brtrue.s   IL_003e

    IL_007d:  ldc.i4.1
    IL_007e:  stloc.2
    IL_007f:  br.s       IL_0081

    IL_0081:  ldloc.2
    IL_0082:  ret
  } // end of method EqArray`2::Equals

} // end of class EqArray`2

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  Main() cil managed
  {
    .entrypoint
    // Code size       313 (0x139)
    .maxstack  7
    .locals init (valuetype EqArray`2<int32,valuetype EqInt> V_0)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    valuetype EqArray`2<int32,valuetype EqInt>
    IL_0009:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_000e:  ldstr      "1: "
    IL_0013:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_0018:  nop
    IL_0019:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_001e:  ldloca.s   V_0
    IL_0020:  ldc.i4.0
    IL_0021:  newarr     [mscorlib]System.Int32
    IL_0026:  ldc.i4.0
    IL_0027:  newarr     [mscorlib]System.Int32
    IL_002c:  call       instance bool valuetype EqArray`2<int32,valuetype EqInt>::Equals(!0[],
                                                                                          !0[])
    IL_0031:  brtrue.s   IL_003a

    IL_0033:  ldstr      "fail"
    IL_0038:  br.s       IL_003f

    IL_003a:  ldstr      "pass"
    IL_003f:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0044:  nop
    IL_0045:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_004a:  ldstr      "2: "
    IL_004f:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_0054:  nop
    IL_0055:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_005a:  ldloca.s   V_0
    IL_005c:  ldc.i4.3
    IL_005d:  newarr     [mscorlib]System.Int32
    IL_0062:  dup
    IL_0063:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::E429CCA3F703A39CC5954A6572FEC9086135B34E
    IL_0068:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_006d:  ldc.i4.3
    IL_006e:  newarr     [mscorlib]System.Int32
    IL_0073:  dup
    IL_0074:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::E429CCA3F703A39CC5954A6572FEC9086135B34E
    IL_0079:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_007e:  call       instance bool valuetype EqArray`2<int32,valuetype EqInt>::Equals(!0[],
                                                                                          !0[])
    IL_0083:  brtrue.s   IL_008c

    IL_0085:  ldstr      "fail"
    IL_008a:  br.s       IL_0091

    IL_008c:  ldstr      "pass"
    IL_0091:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0096:  nop
    IL_0097:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_009c:  ldstr      "3: "
    IL_00a1:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_00a6:  nop
    IL_00a7:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_00ac:  ldloca.s   V_0
    IL_00ae:  ldc.i4.3
    IL_00af:  newarr     [mscorlib]System.Int32
    IL_00b4:  dup
    IL_00b5:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::E429CCA3F703A39CC5954A6572FEC9086135B34E
    IL_00ba:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_00bf:  ldc.i4.2
    IL_00c0:  newarr     [mscorlib]System.Int32
    IL_00c5:  dup
    IL_00c6:  ldc.i4.0
    IL_00c7:  ldc.i4.1
    IL_00c8:  stelem.i4
    IL_00c9:  dup
    IL_00ca:  ldc.i4.1
    IL_00cb:  ldc.i4.2
    IL_00cc:  stelem.i4
    IL_00cd:  call       instance bool valuetype EqArray`2<int32,valuetype EqInt>::Equals(!0[],
                                                                                          !0[])
    IL_00d2:  brtrue.s   IL_00db

    IL_00d4:  ldstr      "pass"
    IL_00d9:  br.s       IL_00e0

    IL_00db:  ldstr      "fail"
    IL_00e0:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_00e5:  nop
    IL_00e6:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_00eb:  ldstr      "4: "
    IL_00f0:  callvirt   instance void [mscorlib]System.IO.TextWriter::Write(string)
    IL_00f5:  nop
    IL_00f6:  call       class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
    IL_00fb:  ldloca.s   V_0
    IL_00fd:  ldc.i4.3
    IL_00fe:  newarr     [mscorlib]System.Int32
    IL_0103:  dup
    IL_0104:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::E429CCA3F703A39CC5954A6572FEC9086135B34E
    IL_0109:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_010e:  ldc.i4.3
    IL_010f:  newarr     [mscorlib]System.Int32
    IL_0114:  dup
    IL_0115:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '<PrivateImplementationDetails>'::'8CFA957D76B6E190580D284C12F31AA6E3E2D41C'
    IL_011a:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_011f:  call       instance bool valuetype EqArray`2<int32,valuetype EqInt>::Equals(!0[],
                                                                                          !0[])
    IL_0124:  brtrue.s   IL_012d

    IL_0126:  ldstr      "pass"
    IL_012b:  br.s       IL_0132

    IL_012d:  ldstr      "fail"
    IL_0132:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    IL_0137:  nop
    IL_0138:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi sealed '<PrivateImplementationDetails>'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=12'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 12
  } // end of class '__StaticArrayInitTypeSize=12'

  .field static assembly initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' '8CFA957D76B6E190580D284C12F31AA6E3E2D41C' at I_00002920
  .field static assembly initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=12' E429CCA3F703A39CC5954A6572FEC9086135B34E at I_00002930
} // end of class '<PrivateImplementationDetails>'


// =============================================================

.data cil I_00002920 = bytearray (
                 04 00 00 00 05 00 00 00 06 00 00 00) 
.data cil I_0000292C = int8[4]
.data cil I_00002930 = bytearray (
                 01 00 00 00 02 00 00 00 03 00 00 00) 
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file C:\roslyn2\concepts\tests\eq_stage2.res
