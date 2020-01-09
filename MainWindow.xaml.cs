﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;


namespace Ha {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        #region Constants & Shit
        public MainWindow() {
            InitializeComponent();
        }

        //Parametry globalne kalkulacji 
        int rows, cols, step;
        double offsetX, offsetY, panicParameter = 0, averageTime = 0, panicStep = 0.1;
        double[,] data, doorData;
        Cell[][] cells, theMostImportantCopyOfCells;
        String buffer;
        int numberOfEvacuations = 0, numberOfIterations = 0;
        Popup pop = new Popup();
        private BackgroundWorker evacuationWorker = null;
        private BackgroundWorker calcWorker = null;
        //private BackgroundWorker panicEvacuationWorker = null;
        public static string path;


        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            pop.Close();
            Application.Current.Shutdown();
            Console.WriteLine("Application has been closed");
        }

        private bool CheckConvertion(string name) {
            int number;

            bool success = Int32.TryParse(name, out number);
            if (success) {
                Console.WriteLine("Converted correctly", name, number);
                return true;
            } else {
                Console.WriteLine("Attempted conversion of '{0}' failed.",
                                    name ?? "<null>");
                return false;
            }
        }
       
        private void ParametersDialog(object sender, RoutedEventArgs e) {
            var dialog = new ParameterWindow();
            if (dialog.ShowDialog() == true) {
                panicParameter = Double.Parse(dialog.panicParameterTB.Text);
            }
        }

        #endregion

        #region Simulations & Heavy Lifting

        private void GenerateFloorField(object sender, RoutedEventArgs e) {
            if (CheckConvertion(rowTb.Text) && CheckConvertion(colTb.Text) == true) {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to generate the floor field?"
                + '\n' + "You will be not allowed to add more stuff to this miserable world you just created.", "Confirm", MessageBoxButton.YesNo);
                switch (result) {
                    case MessageBoxResult.Yes:
                        Cell.GenerateField(cells);

                        obst.IsEnabled = false;
                        door.IsEnabled = false;
                        people.IsEnabled = false;
                        evacuateHoomansBtn.IsEnabled = true;
                        evacuateHoomansNTimesBtn.IsEnabled = true;
                        multiplePanicParametersButton.IsEnabled = true;
                        if(Cell.DoorsCount(cells) == 1)
                            widerDoorButton.IsEnabled = true;

                        Label floorValueLabel;
                        for (int i = 0; i < cols; i++) {
                            for (int j = 0; j < rows; j++) {

                                floorValueLabel = new Label {
                                    Content = cells[i][j].floorValue,
                                    Width = step,
                                    Height = step
                                };

                                if (step <= 100) {
                                    floorValueLabel.FontSize = step * 3 / 12;
                                } else {
                                    floorValueLabel.FontSize = step * 3 / 16;
                                }

                                Canvas.SetLeft(floorValueLabel, cells[i][j].x);
                                Canvas.SetTop(floorValueLabel, cells[i][j].y);
                                canvas.Children.Add(floorValueLabel);
                            }
                        }
                        evacuateHoomansBtn.IsEnabled = true;

                        //Cell.FindHoomans(cells);
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }

            theMostImportantCopyOfCells = Cell.DeepCopy(cells);
        }

        private string SaveArrayToFile(string fileName, double[,] array, string text) {
           
            System.Windows.Forms.FolderBrowserDialog openfiledalog = new System.Windows.Forms.FolderBrowserDialog();
            if (openfiledalog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {

                var dest = Path.Combine(openfiledalog.SelectedPath, fileName);

                using (StreamWriter file = new StreamWriter(dest, false)) {
                    for (int j = 0; j < array.GetLength(1); j++) {
                        for (int i = 0; i < array.GetLength(0); i++) {
                            file.Write(array[i,j].ToString() + '\t');
                        }
                        file.Write("\n");
                    }
                    MessageBox.Show("Data has been saved", text);
                }

            }
            return openfiledalog.SelectedPath;
        }

        private void SaveSimulatedData(object sender, RoutedEventArgs e) {

            String fileName = DateTime.Now.ToString("h/mm/ss_tt");
            System.Windows.Forms.FolderBrowserDialog openfiledalog = new System.Windows.Forms.FolderBrowserDialog();
            if (openfiledalog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {

                var dest = Path.Combine(openfiledalog.SelectedPath, "floor_" + fileName + ".txt");

                using (StreamWriter file = new StreamWriter(dest, false)) {
                    for (int j = 0; j < rows; j++) {
                        for (int i = 0; i < cols; i++) {
                            file.Write(cells[i][j].floorValue.ToString() + '\t');
                        }
                        file.Write("\n");
                    }
                    dest = Path.Combine(openfiledalog.SelectedPath, "hoomans_" + fileName + ".txt");
                    using (StreamWriter file2 = new StreamWriter(dest, false)) {
                        file2.Write(buffer);
                    }

                    dest = Path.Combine(openfiledalog.SelectedPath, "density_" + fileName + ".txt");
                    using (StreamWriter file3 = new StreamWriter(dest, false)) {
                        for (int j = 0; j < rows; j++) {
                            for (int i = 0; i < cols; i++) {
                                file3.Write(cells[i][j].howManyHoomansWereThere.ToString() + '\t');
                            }
                            file3.Write("\n");
                        }
                    }
                }
            }
        }

        int EvacuationCalc(bool doYouWantBackgroundWorker, Cell[][] fieldArray, double panicParameter) {       //ewakuuje wszystkich i zwraca ilosc iteracji potrzebnych do ewakuacji
            int counter = 0;
            int numberOfIterations = 0;
            buffer += "Time:" + counter.ToString() + " iterations" + '\n';
            for (int j = 0; j < rows; j++) {
                for (int i = 0; i < cols; i++) {
                    if (fieldArray[i][j].isAWall)
                        buffer += "#";
                    else if (fieldArray[i][j].isAPerson)
                        buffer += "1";
                    else if (fieldArray[i][j].isADoor)
                        buffer += "D";
                    else
                        buffer += "0";
                    buffer += '\t';
                }
                buffer += '\n';
            }
            List<Cell> listOfHoomans = Cell.FindHoomans(fieldArray);
            while (listOfHoomans.Count != 0) {
                counter++;
                numberOfIterations++;
                buffer += "Time: " + counter.ToString() + " iterations" + '\n';
                
                Cell evacuateTo;
                foreach (Cell cell in listOfHoomans) {
                    cell.howManyHoomansWereThere += 1;
                    evacuateTo = Cell.FindNeighbour(cell, fieldArray, panicParameter);       //znajdujemy pozycje gdzie ma sie ewakuowac
                    fieldArray[cell.i][cell.j].isAPerson = false;            //likwidujemy ludzika z miejsca gdzie stal
                    fieldArray[evacuateTo.i][evacuateTo.j].isAPerson = true; //i wstawiamy go tam gdzie ma sie ewakuowac

                }

                Random rng = new Random();  //szuffle
                int n = listOfHoomans.Count;
                while (n > 1) {
                    n--;
                    int k = rng.Next(n + 1);
                    Cell value = listOfHoomans[k];
                    listOfHoomans[k] = listOfHoomans[n];
                    listOfHoomans[n] = value;
                }

                for (int j = 0; j < rows; j++) {
                    for (int i = 0; i < cols; i++) {
                        if (fieldArray[i][j].isAWall)
                            buffer += "#" + '\t';
                        else if (fieldArray[i][j].isAPerson)
                            buffer += "1" + '\t';
                        else if (fieldArray[i][j].isADoor)
                            buffer += "D" + '\t';
                        else
                            buffer += "0" + '\t';
                    }
                    buffer += '\n';
                }

                if (doYouWantBackgroundWorker) {
                    evacuationWorker.ReportProgress(1);
                    Thread.Sleep(500);
                }

                listOfHoomans = Cell.FindHoomans(fieldArray);
            }

            return numberOfIterations;
        }

        private void EvacuateHoomans(object sender, RoutedEventArgs e) {
            if (null == evacuationWorker) {
                evacuationWorker = new BackgroundWorker();
                evacuationWorker.DoWork += new DoWorkEventHandler(evacuationWorker_DoWork);
                evacuationWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(evacuationWorker_RunWorkerCompleted);
                evacuationWorker.ProgressChanged += new ProgressChangedEventHandler(evacuationWorker_ProgressChanged);
                evacuationWorker.WorkerReportsProgress = true;
                evacuationWorker.WorkerSupportsCancellation = true;
            }
            if (!evacuationWorker.IsBusy) {
                evacuationWorker.RunWorkerAsync();
                saveItem.IsEnabled = true;
            }
        }

        private void EvacuateHoomansNTimesBtn_Click(object sender, RoutedEventArgs e) {

            var dialog = new MoreCalculationParameters();
            if (dialog.ShowDialog() == true) {
                numberOfEvacuations = Int32.Parse(dialog.numevacTb.Text);
                Console.WriteLine("number of evacuations " + numberOfEvacuations);
                panicParameter = Double.Parse(dialog.panicParTb.Text);

                dialog.Close();
                if (null == calcWorker) {
                    calcWorker = new BackgroundWorker();
                    calcWorker.DoWork += new DoWorkEventHandler(calcWorker_DoWork);
                    calcWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(calcWorker_RunWorkerCompleted);
                    calcWorker.ProgressChanged += new ProgressChangedEventHandler(calcWorker_ProgressChanged);
                    calcWorker.WorkerReportsProgress = true;
                    calcWorker.WorkerSupportsCancellation = true;
                }
                if (!calcWorker.IsBusy) {
                    calcWorker.RunWorkerAsync();
                    pop.Show();
                }
            }
        }

        private void multiplePanicParametersButton_Click(object sender, RoutedEventArgs e) {
            panicParameter = 0;

            var dialog = new GraphSettings();
            if (dialog.ShowDialog() == true) {
                numberOfEvacuations = Int32.Parse(dialog.TbNumberOfEvacuations.Text);
                Console.WriteLine("number of evacuations " + numberOfEvacuations);
                panicStep = Double.Parse(dialog.TbStep.Text);
            }
            dialog.Close();

            calcWorker = new BackgroundWorker();
            calcWorker.DoWork += new DoWorkEventHandler(panicEvacuationWorker_DoWork);
            calcWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(panicEvacuationWorker_RunWorkerCompleted);
            calcWorker.ProgressChanged += new ProgressChangedEventHandler(calcWorker_ProgressChanged);
            calcWorker.WorkerReportsProgress = true;
            calcWorker.WorkerSupportsCancellation = true;

            if (!calcWorker.IsBusy) {
                calcWorker.RunWorkerAsync();
                pop.Show();
            }
        }

        private void widerDoorButton_Click(object sender, RoutedEventArgs e) {

        }

        #endregion
    }
}