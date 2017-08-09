

namespace ObserverPattern {
  using System.Collections.Generic;
  //adapted from JavaGI: Generalized Interface for Java.

  concept ObserverPattern<Subject, Observer> {

    //Subject
    List<Observer> GetObservers(Subject s);
    void Register(Subject s, Observer o); // with default implementation DefaultRegister
    void Notify(Subject s);               // with default implementation DefaultNotify

    //Observer

    void Update(Observer o, Subject s);
  }

  static class Overloads {
    public static List<Observer> GetObservers<Subject, Observer, implicit OP>(Subject s) where OP : ObserverPattern<Subject, Observer> {
      return GetObservers(s);
    }

    public static void Register<Subject, Observer, implicit OP>(Subject s, Observer o) where OP : ObserverPattern<Subject, Observer> {
      Register(s, o);
    }

    public static void Notify<Subject, Observer, implicit OP>(Subject s) where OP : ObserverPattern<Subject, Observer> {
      Notify(s);
    }

    public static void DefaultRegister<Subject, Observer, implicit OP>(Subject s, Observer o) where OP : ObserverPattern<Subject, Observer> {
      GetObservers<Subject, Observer, OP>(s).Add(o);
    }

    public static void DefaultNotify<Subject, Observer, implicit OP>(Subject s) where OP : ObserverPattern<Subject, Observer> {
      foreach (Observer o in GetObservers<Subject, Observer, OP>(s))
        Overloads.Update(o, s);
    }

    public static void Update<Subject, Observer, implicit OP>(Observer o, Subject s) where OP : ObserverPattern<Subject, Observer> {
      Update(o, s);
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

  instance ObserverPatternModelDisplay : ObserverPattern<Model, Display> {
    List<Display> ObserverPattern<Model, Display>.GetObservers(Model s) {
      return s.GetObservers();
    }

    void ObserverPattern<Model, Display>.Register(Model m, Display s) {
      Overloads.DefaultRegister<Model, Display, ObserverPatternModelDisplay>(m, s);
    }

    void ObserverPattern<Model, Display>.Notify(Model m) {
      Overloads.DefaultNotify<Model, Display, ObserverPatternModelDisplay>(m);
    }

    void ObserverPattern<Model, Display>.Update(Display o, Model s) {
      o.Update(s);
    }
  }

  instance ObserverPatternModelDisplay2 : ObserverPattern<Model, Display> {
    public List<Display> GetObservers(Model s) {
      return s.GetObservers();
    }

    public void Register(Model m, Display s) {
      System.Console.WriteLine("Register from OPMD2");
      Overloads.DefaultRegister<Model, Display, ObserverPatternModelDisplay2>(m, s);
    }

    public void Notify(Model m) {
      System.Console.WriteLine("Notify from OPMD2");
      Overloads.DefaultNotify<Model, Display, ObserverPatternModelDisplay2>(m);
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
        Overloads.Register<Model, Display, ObserverPatternModelDisplay>(m, d);
        Overloads.Register<Model, Display, ObserverPatternModelDisplay>(m, e);
        Overloads.Notify<Model, Display, ObserverPatternModelDisplay>(m);
      }


      {
        Model m = new Model();
        Display d = new Display();
        Display e = new Display();
        Overloads.Register<Model, Display, ObserverPatternModelDisplay2>(m, d);
        Overloads.Register<Model, Display, ObserverPatternModelDisplay2>(m, e);
        Overloads.Notify<Model, Display, ObserverPatternModelDisplay2>(m);
      }
    }


  }


}

