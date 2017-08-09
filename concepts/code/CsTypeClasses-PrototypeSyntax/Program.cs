using System;

class Program {

  static void Main() {
  //    System.Diagnostics.Debugger.Launch();
  //    System.Diagnostics.Debugger.Break();
    Eq.Test.Run();
    Num.Test.Run();
    NumEq.Test.Run();
    Lists.Test.Run();
    ListsEq.Test.Run();
    Exp.Test.Run();
    ObserverPattern.Test.Run();
    Existentials.Test.Run();
    OpEq.Test.Run();
    OpNum.Test.Run();
    Perf.Gsort.Run(new string[] { "1000000" });
    Console.ReadLine();
  }
}