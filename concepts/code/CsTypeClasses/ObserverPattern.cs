

namespace ObserverPattern {
  using System.Collections.Generic;
  //adapted from JavaGI: Generalized Interface for Java.

  interface ObserverPattern<Subject, Observer> {

    //Subject
    List<Observer> GetObservers(Subject s);
    void Register(Subject s, Observer o); // with default implementation DefaultRegister
    void Notify(Subject s);               // with default implementation DefaultNotify

    //Observer

    void Update(Observer o, Subject s);
  }

  static class Overloads {
    public static List<Observer> GetObservers<OP, Subject, Observer>(Subject s) where OP : struct, ObserverPattern<Subject, Observer> {
      return default(OP).GetObservers(s);
    }

    public static void Register<OP, Subject, Observer>(Subject s, Observer o) where OP : struct, ObserverPattern<Subject, Observer> {
      default(OP).Register(s, o);
    }

    public static void Notify<OP, Subject, Observer>(Subject s) where OP : struct, ObserverPattern<Subject, Observer> {
      default(OP).Notify(s);
    }

    public static void DefaultRegister<OP, Subject, Observer>(Subject s, Observer o) where OP : struct, ObserverPattern<Subject, Observer> {
      GetObservers<OP, Subject, Observer>(s).Add(o);

    }

    public static void DefaultNotify<OP, Subject, Observer>(Subject s) where OP : struct, ObserverPattern<Subject, Observer> {
      foreach (Observer o in GetObservers<OP, Subject, Observer>(s))
        Update<OP, Subject, Observer>(o, s);
    }

    public static void Update<OP, Subject, Observer>(Observer o, Subject s) where OP : struct, ObserverPattern<Subject, Observer> {
      default(OP).Update(o, s);
    }
  }

  class Model {
    private List<Display> observers = new List<Display>();
    internal List<Display> GetObservers() {
      return observers;
    }
  }

  class Display {
    internal void Update(Model m) { System.Console.WriteLine("model has changed"); }
  }

  struct ObserverPatternModelDisplay : ObserverPattern<Model, Display> {
    List<Display> ObserverPattern<Model, Display>.GetObservers(Model s) {
      return s.GetObservers();
    }

    void ObserverPattern<Model, Display>.Register(Model m, Display s) {
      Overloads.DefaultRegister<ObserverPatternModelDisplay, Model, Display>(m, s);
    }

    void ObserverPattern<Model, Display>.Notify(Model m) {
      Overloads.DefaultNotify<ObserverPatternModelDisplay, Model, Display>(m);
    }

    void ObserverPattern<Model, Display>.Update(Display o, Model s) {
      o.Update(s);
    }
  }

  struct ObserverPatternModelDisplay2 : ObserverPattern<Model, Display> {
    public List<Display> GetObservers(Model s) {
      return s.GetObservers();
    }

    public void Register(Model m, Display s) {
      System.Console.WriteLine("Register from OPMD2");
      Overloads.DefaultRegister<ObserverPatternModelDisplay, Model, Display>(m, s);
    }

    public void Notify(Model m) {
      System.Console.WriteLine("Notify from OPMD2");
      Overloads.DefaultNotify<ObserverPatternModelDisplay, Model, Display>(m);
    }

    public void Update(Display o, Model s) {
      o.Update(s);
    }
  }
  class Test {

    public static void Run() {

      {
        Model m = new Model();
        Display d = new Display();
        Display e = new Display();
        Overloads.Register<ObserverPatternModelDisplay, Model, Display>(m, d);
        Overloads.Register<ObserverPatternModelDisplay, Model, Display>(m, e);
        Overloads.Notify<ObserverPatternModelDisplay, Model, Display>(m);
      }


      {
        Model m = new Model();
        Display d = new Display();
        Display e = new Display();
        Overloads.Register<ObserverPatternModelDisplay2, Model, Display>(m, d);
        Overloads.Register<ObserverPatternModelDisplay2, Model, Display>(m, e);
        Overloads.Notify<ObserverPatternModelDisplay2, Model, Display>(m);
      }
    }


  }


}

