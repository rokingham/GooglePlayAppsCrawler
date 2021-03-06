﻿using System;
using System.Linq;
using SharedLibrary;
using SharedLibrary.MongoDB;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using SharedLibrary.Models;
using MongoDB.Driver.Builders;
using NLog;

namespace AppsExporter
{
    class Program
    {
        ///  *** READ THIS BEFORE YOU START. ***
        ///  *** I MEAN IT, PLEASE, READ IT  ***
        /// 
        ///  This exporting helper will download ALL THE APPS found on the database, and
        ///  dump it to a CSV file (with headers).
        ///  
        ///  Note that, since the database is Hosted on AWS, i will PAY (for the internet outbound traffic) if you execute a full database export,
        ///  so, if you are going to execute a full export, please, get in touch with me before running this project, or send me a donation
        ///  via paypal on marcello.grechi@gmail.com
        ///  
        ///  Also, be nice with the database.
        ///  
        ///  ** END OF WARNING ***

        static void Main (string[] args)
        {
            // Logs Counter
            int processedApps = 0;

            // Configuring Log Object
            Logger logger = LogManager.GetCurrentClassLogger ();
            logger.Info ("Worker Started");

            logger.Info ("Checking Arguments");
            
            // Periodic Log Timer
            Timer loggingThread = new Timer((TimerCallback) =>
            {
                logger.Info ("Processed Apps: " + processedApps);

            }, null, 10000, 10000);
            
            // Validating Arguments
            if (!ValidateArgs (args))
            {
                logger.Fatal ("Invalid Args", "Args must have 1 element");
                return;
            }

            logger.Info ("Checking Write Permissions on output Path");
            // Validating Write Permissions on output path
            if (!ValidateFilePermissions (args[0]))
            {
                logger.Fatal ("Insuficient Permissions", "Cannot write on path : " + args[0]);
                return;
            }

            // Configuring MongoDB Wrapper
            MongoDBWrapper mongoDB = new MongoDBWrapper();
            string fullServerAddress = String.Join(":", Consts.MONGO_SERVER, Consts.MONGO_PORT);
            mongoDB.ConfigureDatabase(Consts.MONGO_USER, Consts.MONGO_PASS, Consts.MONGO_AUTH_DB, fullServerAddress, Consts.MONGO_TIMEOUT, Consts.MONGO_DATABASE, Consts.MONGO_COLLECTION);
            
            // Opening Output Stream
            using (StreamWriter sWriter = new StreamWriter (args[0], true, Encoding.GetEncoding("ISO-8859-1")))
            {
                // Auto Flush Content
                sWriter.AutoFlush = true;

                // Writing Headers
                String headersLine = "Url,ReferenceDate,Name,Developer,IsTopDeveloper,DeveloperURL,PublicationDate,"
                                   + "Category,IsFree,Price,Reviewers,Score.Total,Score.Count,Score.FiveStars,"
                                   + "Score.FourStars,Score.ThreeStars,Score.TwoStars,Score.OneStars,LastUpdateDate"
                                   + "AppSize,Instalations,CurrentVersion,MinimumOSVersion,ContentRating,HaveInAppPurchases,DeveloperEmail,DeveloperWebsite,DeveloperPrivacyPolicy";

                sWriter.WriteLine (headersLine);

                // Example of MongoDB Query Construction
                // Queries for records which have the attribute "IsTopDeveloper" equal to "false"
                //var mongoQuery = Query.EQ ("IsTopDeveloper", false);
                var mongoQuery = Query.EQ ("Category", "/store/apps/category/SPORTS");

                // More Examples of Queries
                // var mongoQuery = Query.EQ ("Category", "/store/apps/category/GAME_CASINO");
                // var mongoQuery = Query.GT ("Price", 10);

                // Reading all apps from the database
                // USAGE: CHANGE FindMatches to FindAll if you want to export all the records from the database
                foreach (AppModel app in mongoDB.FindMatch<AppModel>(mongoQuery))
                {
                    try
                    {
                        // Writing line to File
                        sWriter.WriteLine (app.ToString ());
                        processedApps++;
                    }
                    catch (Exception ex)
                    {
                        logger.Error (ex);
                    }
                }
            }

            // Logging end of the Process
            logger.Info ("Finished Exporting Database");
        }

        private static bool ValidateArgs (string[] args)
        {
            return args.Length == 1;
        }

        private static bool ValidateFilePermissions (string filePath)
        {
            string directoryName = Directory.GetParent (filePath).FullName;

            PermissionSet permissionSet = new PermissionSet (PermissionState.None);

            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, directoryName);

            permissionSet.AddPermission(writePermission);

            if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
