using System;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using MpiLibrary;

namespace DotNetClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting up an MPI program!");

            var dataFilePath = args[0];

            // Start up MPI
            // We can wrap the MPI helper library in a using statement so that we can instantiate the MPI connection
            // and have MPI_Finalize automatically called when we are done.
            using (var mpi = new Mpi(args))
            {
                // How big is the cluster, and which node are we?
                int worldSize = mpi.GetWorldSize();
                int worldRank = mpi.GetWorldRank();

                Console.WriteLine($"Rank: {worldRank}, Size: {worldSize}");

                // Do a quick allreduce to show that everything works
                int sum = 0;
                mpi.AllReduce(worldRank, ref sum);
                Console.WriteLine($"AllReduce Rank-Sum: {sum}");

                // Now let's give a little demo showing how we can cooperatively build a model.
                Console.WriteLine("Training a linear model....");
                TrainLinearModel(dataFilePath, (uint)worldRank, out float[] biasAndWeights);

                var averageBiasAndWeights = new float[biasAndWeights.Length];
                mpi.AllReduce(biasAndWeights, averageBiasAndWeights);
                for (int i = 0; i < averageBiasAndWeights.Length; i++)
                    averageBiasAndWeights[i] /= (float)worldSize;

                Console.WriteLine($"Rank-{worldRank}: bias={biasAndWeights[0]} weight[0]={biasAndWeights[1]} | bias={averageBiasAndWeights[0]} weight[0]={averageBiasAndWeights[1]}");
            }
        }

        /// <summary>
        /// Train a linear model and return the bias and weights as an array.
        /// </summary>
        /// <remarks>
        /// Mostly copied from the ML.NET samples, e.g. https://github.com/dotnet/machinelearning/blob/master/docs/samples/Microsoft.ML.Samples/Dynamic/GeneralizedAdditiveModels.cs
        /// </remarks>
        /// <param name="dataFilePath">The path to the housing regression data file.</param>
        /// <param name="seed">Seed for the random number generator</param>
        /// <param name="biasAndWeights">The bias and weights of the model</param>
        static void TrainLinearModel(string dataFilePath, uint seed, out float[] biasAndWeights)
        {
            // Create a new context for ML.NET operations. It can be used for exception tracking and logging, 
            // as a catalog of available operations and as the source of randomness.
            var mlContext = new MLContext();

            // Step 1: Read the data as an IDataView.
            // First, we define the reader: specify the data columns and where to find them in the text file.
            var reader = mlContext.Data.CreateTextReader(
                columns: new[]
                    {
                        new TextLoader.Column("MedianHomeValue", DataKind.R4, 0),
                        new TextLoader.Column("CrimesPerCapita", DataKind.R4, 1),
                        new TextLoader.Column("PercentResidental", DataKind.R4, 2),
                        new TextLoader.Column("PercentNonRetail", DataKind.R4, 3),
                        new TextLoader.Column("CharlesRiver", DataKind.R4, 4),
                        new TextLoader.Column("NitricOxides", DataKind.R4, 5),
                        new TextLoader.Column("RoomsPerDwelling", DataKind.R4, 6),
                        new TextLoader.Column("PercentPre40s", DataKind.R4, 7),
                        new TextLoader.Column("EmploymentDistance", DataKind.R4, 8),
                        new TextLoader.Column("HighwayDistance", DataKind.R4, 9),
                        new TextLoader.Column("TaxRate", DataKind.R4, 10),
                        new TextLoader.Column("TeacherRatio", DataKind.R4, 11),
                    },
                hasHeader: true
            );
            // Read the data
            var data = reader.Read(dataFilePath);

            var labelName = "MedianHomeValue";
            var featureNames = data.Schema
                .Select(column => column.Name) // Get the column names
                .Where(name => name != labelName) // Drop the Label
                .ToArray();

            // Sample with TrainTest split with different seeds
            var (trainSet, testSet) = mlContext.Regression.TrainTestSplit(data, testFraction: 0.2, seed: seed);

            // Step 2: Pipeline
            // Concatenate the features to create a Feature vector.
            // Normalize the data set so that for each feature, its maximum value is 1 while its minimum value is 0.
            // Then append a linear regression trainer.
            var pipeline = mlContext.Transforms.Concatenate("Features", featureNames)
                    .Append(mlContext.Regression.Trainers.StochasticDualCoordinateAscent(
                        labelColumn: labelName, featureColumn: "Features"));
            var model = pipeline.Fit(trainSet);

            biasAndWeights = new float[1 + featureNames.Length];

            // Extract the model from the pipeline
            var linearPredictor = model.LastTransformer;
            biasAndWeights[0] = linearPredictor.Model.Bias;
            linearPredictor.Model.Weights.ToArray().CopyTo(biasAndWeights, 1);
        }
    }
}
