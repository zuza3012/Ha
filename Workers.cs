﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;


namespace Ha {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        void evacuationWorker_DoWork(object sender, DoWorkEventArgs e) {
            numberOfIterations = EvacuationCalc(true, cells, panicParameter);
            
            evacuationWorker.ReportProgress(100);
        }

        void evacuationWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            for (int i = 1; i < cols - 1; i++) {
                for (int j = 1; j < rows - 1; j++) {
                    DrawSomething(i, j);
                    DrawLabel(i, j);
                }
            }
        }

        void evacuationWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            MessageBox.Show("Evacuation completed succesfully." + "\n" + "Evacuation time: " + numberOfIterations + " iterations", "You are the real hero!");
            cells = Cell.DeepCopy(theMostImportantCopyOfCells);

            System.Threading.Thread.Sleep(500);
            for (int i = 1; i < cols - 1; i++) {                //wracamy do ulozenia poczatkowego
                for (int j = 1; j < rows - 1; j++) {
                    DrawSomething(i, j);
                    DrawLabel(i, j);
                }
            }
        }

        // Drugi backgroundWorker do wielu symulacji "w tle" poprzez przycisk Simulate N Evacuations 

        void calcWorker_DoWork(object sender, DoWorkEventArgs e) {
            double sum = 0;
            for (int i = 0; i < numberOfEvacuations; i++) {
                Cell[][] copyCells = Cell.DeepCopy(cells);
                sum += EvacuationCalc(false, copyCells, panicParameter);
                calcWorker.ReportProgress((int)(100 * i / (double)numberOfEvacuations));
            }

            averageTime = sum / numberOfEvacuations;
            calcWorker.ReportProgress(100);
        }

        void calcWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            pop.pgBar.Value = e.ProgressPercentage;
        }

        void calcWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            pop.Hide();
            MessageBox.Show("Simulations completed! " + "\n" + "Average evacuation time: " + averageTime + " iterations", "You are the real hero!");
        }



        void panicEvacuationWorker_DoWork(object sender, DoWorkEventArgs e) {
            int k = (int)(1 / panicStep);
            data = new double[2, k];
            panicParameter = 0;
            calcWorker.ReportProgress(0);
            for (int j = 0; j < k; j++) {
                double sum = 0;
                for (int i = 0; i < numberOfEvacuations; i++) {
                    Cell[][] copyCells = Cell.DeepCopy(cells);
                    sum += EvacuationCalc(false, copyCells, panicParameter);
                    calcWorker.ReportProgress((int)(100 * (double)j / k) + i);
                }

                data[1, j] = sum / numberOfEvacuations;
                data[0, j] = panicParameter;
                panicParameter += panicStep;
            }
            calcWorker.ReportProgress(100);
        }


        private void panicEvacuationWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            pop.Hide();
            MessageBox.Show("Simulations completed! ", "You are the real hero!");
            string fileName = "graphData_" + DateTime.Now.ToString("h/mm/ss_tt");
            path = SaveArrayToFile(fileName + ".txt", data, "Simulations saved") + @"\" + fileName + ".txt";
            Graph graph = new Graph(1);
            graph.Show();
        }

        private void widerDoor_DoWork(object sender, DoWorkEventArgs e) {
            List<Cell[][]> cellsWithWiderDoors = Cell.WiderDoor(cells);
            int l = cellsWithWiderDoors.Count;

            doorData = new double[2,l];

            int counter = 0;
            foreach(Cell[][] copy in cellsWithWiderDoors) {
                double sum = 0;
                for (int k = 0; k < numberOfEvacuations; k++) {
                    sum += EvacuationCalc(false, copy, panicParameter);
                    calcWorker.ReportProgress((int)(100 * (double)counter / l + k));
                }
                doorData[0, counter] = counter + 1;
                doorData[1, counter] = sum / numberOfEvacuations;
                counter++;
                
            }
            calcWorker.ReportProgress(100);
            

        }

        private void widerDoor_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            pop.Hide();
            MessageBox.Show("Data has been created!", "You are the real hero!");
            
            string fileName = "Data_" + DateTime.Now.ToString("h/mm/ss_tt");
            path2 = SaveArrayToFile(fileName + ".txt", doorData, "Simulations saved") + @"\" + fileName + ".txt";
            Graph graph = new Graph(2);
            graph.Show();
        }
    }
}