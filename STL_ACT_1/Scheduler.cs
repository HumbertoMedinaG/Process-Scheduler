﻿using STL_ACT_1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scheduler
{
  class Scheduler
  {
    public int GlobalTime { get; set; }
    public Queue<Process> New { get; set; }
    public Queue<Process> Ready { get; set; }
    public Process Running { get; set; }
    public Queue<Process> Blocked { get; set; }
    public Queue<Process> Terminated { get; set; }

    private static readonly Random r = new Random();
    private const int MEMORY_LIMIT = 5;
    private bool wasInterru;
    private bool wasBlocked;

    MainWindow mW;

    public Scheduler(MainWindow mW)
    {
      GlobalTime = 0;
      New = new Queue<Process>();
      Ready = new Queue<Process>();
      Running = new Process();
      Blocked = new Queue<Process>();
      Terminated = new Queue<Process>();
      this.mW = mW;
    }

    public async void StartProcessing()
    {
      Admit();
      while (Ready.Count > 0 || Blocked.Count > 0) {
        // Reset variables
        wasInterru = false;
        wasBlocked = false;

        Admit();
        Dispatch();

        // --------- WINDOW ----------- //
        mW.UpdateLabels(Running);
        mW.UpdateTable(New, mW.tblNew); // TODO
        mW.UpdateTable(Ready, mW.tblReady);
        mW.UpdateTable(Terminated, mW.tblTerminated);
        // ---------------------------- //

        await ExecuteRunning();

        if (!wasBlocked && Running.Exists) {
          Exit();
        }
      }
      mW.UpdateTable(Terminated, mW.tblTerminated); // TODO
      mW.UpdateTable(Terminated, mW.tblTimes);
    }

    public void Admit()
    {
      int countRunning = Running.Exists ? 1 : 0;
      while (New.Count > 0 && Ready.Count + Blocked.Count + countRunning < MEMORY_LIMIT) {
        Running.tLle = GlobalTime;
        Ready.Enqueue(New.Dequeue());
      }
    }

    public void Dispatch()
    {
      if (Ready.Count > 0) {
        Running = Ready.Dequeue();
        Running.Exists = true;
        Running.tResp = GlobalTime;
      } else {
        Running = new Process();
      }
    }

    public void Interrupt()
    {
      Blocked.Enqueue(Running);
      Running.Exists = false;
    }

    public void Deinterrupt()
    {
      Ready.Enqueue(Blocked.Dequeue());
    }

    public void Exit()
    {
      Running.tFin = GlobalTime;
      Running.tRet = Running.tFin - Running.tLle;
      Terminated.Enqueue(Running);
      Running.Exists = false;
    }

    private async Task ExecuteRunning()
    {
      if (Running.Exists) {
        while (Running.tTra < Running.TME) {
          // Stops for a second
          await Task.Delay(1000);
          // Increase time for all processes
          IncreaseTime();
          // Update process remaining time
          Running.tRest = Running.TME - Running.tTra;

          await WasKeyPressed();

          // --------- WINDOW ----------- //
          mW.UpdateLabels(Running);
          mW.UpdateTable(Blocked, mW.tblBlocked);
          // ---------------------------- //

          if (wasBlocked || wasInterru) { return; }
        }
      } else {
        // Stops for a second
        await Task.Delay(1000);
        // Increase time for all processes
        IncreaseTime();
      }
    }

    private void IncreaseTime()
    {
      // Increase Global Time
      GlobalTime++;
      // Increase Running Times
      if (Running.Exists) { Running.tTra++; }
      // Icrease waiting time to all ready processes 
      foreach (Process p in Ready) {
        p.tEsp++;
      }
      // Icrease blocked time to all blocked processes 
      bool DeInterrupt = false;
      foreach (Process p in Blocked) {
        p.tEsp++;
        p.tBlo++;
        if (p.tBlo >= 8) {
          DeInterrupt = true;
          p.tBlo = 0;
        }
      }
      if (DeInterrupt) {
        mW.tblBlocked.Items.Remove(Blocked.Peek());
        mW.tblReady.Items.Add(Blocked.Peek());
        Deinterrupt();
      }
    }

    public void CreateProcesses(int totalProcesses)
    {
      for (int id = 1; id <= totalProcesses; id++) {
        CreateProcess(id);
      }
    }

    private void CreateProcess(int ID)
    {
      // Random values for the process
      int TME = r.Next(8, 18);
      int num1 = r.Next(0, 100);
      int opeIdx = r.Next(0, 5);
      int num2 = r.Next(0, 100);

      if (opeIdx == 3 || opeIdx == 4) { num2++; }
      var Ope = new Operation(num1, opeIdx, num2);
      New.Enqueue(new Process(ID, TME, Ope));
    }

    private async Task WasKeyPressed()
    {
      switch (mW.KeyPressed) {
        case "I":
          wasBlocked = true;
          Interrupt();
          break;
        case "E":
          Running.OpeResult = "ERROR!";
          wasInterru = true;
          break;
        case "P":
          while (mW.KeyPressed != "C") {
            await Task.Delay(1000);
          }
          break;
      }
      mW.KeyPressed = "";
    }
  }
}