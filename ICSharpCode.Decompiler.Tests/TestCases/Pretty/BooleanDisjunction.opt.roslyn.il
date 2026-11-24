




// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly BooleanDisjunction
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- 下列自定义特性会自动添加，不要取消注释 -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .permissionset reqmin
             = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module BooleanDisjunction.dll
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.Pretty.BooleanDisjunction
       extends [mscorlib]System.Object
{
  .method public hidebysig static bool  RangeTable(uint32 cp) cil managed
  {
    // 代码大小       37 (0x25)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x300
    IL_0006:  blt.un.s   IL_0023

    IL_0008:  ldarg.0
    IL_0009:  ldc.i4     0x36f
    IL_000e:  ble.un.s   IL_0021

    IL_0010:  ldarg.0
    IL_0011:  ldc.i4     0x670
    IL_0016:  beq.s      IL_0021

    IL_0018:  ldarg.0
    IL_0019:  ldc.i4     0xd00
    IL_001e:  ceq
    IL_0020:  ret

    IL_0021:  ldc.i4.1
    IL_0022:  ret

    IL_0023:  ldc.i4.0
    IL_0024:  ret
  } // end of method BooleanDisjunction::RangeTable

  .method public hidebysig static bool  GuardedLastOperand(uint32 cp) cil managed
  {
    // 代码大小       40 (0x28)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x36f
    IL_0006:  ble.un.s   IL_0026

    IL_0008:  ldarg.0
    IL_0009:  ldc.i4     0x670
    IL_000e:  beq.s      IL_0026

    IL_0010:  ldarg.0
    IL_0011:  ldc.i4     0xe0100
    IL_0016:  blt.un.s   IL_0024

    IL_0018:  ldarg.0
    IL_0019:  ldc.i4     0xe01ef
    IL_001e:  cgt.un
    IL_0020:  ldc.i4.0
    IL_0021:  ceq
    IL_0023:  ret

    IL_0024:  ldc.i4.0
    IL_0025:  ret

    IL_0026:  ldc.i4.1
    IL_0027:  ret
  } // end of method BooleanDisjunction::GuardedLastOperand

} // end of class ICSharpCode.Decompiler.Tests.TestCases.Pretty.BooleanDisjunction


// =============================================================

// *********** 反汇编完成 ***********************
