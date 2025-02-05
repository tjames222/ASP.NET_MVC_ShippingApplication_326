﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Serilog;

namespace CST_326_CLC.Models
{
    public class ShipmentModel
    {
        public int ShipmentId { get; set; }
        // Shall be used for user lookup options and delivery status updates
        public string Status { get; set; }
        // Shall be used for standard package sizes if Packaging type is STANDARD (Small, Medium, Large)
        public string PackageSize { get; set; }
        [Required(ErrorMessage = "The Weight field is required.")]
        public int Weight { get; set; }
        [Required(ErrorMessage = "The Height field is required.")]
        public int Height { get; set; }
        [Required(ErrorMessage = "The Width field is required.")]
        public int Width { get; set; }
        [Required(ErrorMessage = "The Length field is required.")]
        public int Length { get; set; }
        [Required(ErrorMessage = "The Zip field is required.")]
        public int Zip { get; set; }
        // The Packaging type will determine wether or not the user needs to input dimensions
        [Required(ErrorMessage = "The Packaging Type field is required.")]
        public bool IsPackageStandard { get; set; }
        // Delivery Option shall be three choices (Ground 4-5 days, Next Day, Standard 1-3 days)
        [Required(ErrorMessage = "The Delivery Option field is required.")]
        public string DeliveryOption { get; set; }
        // Business prices should be higher
        public bool IsResidential { get; set; }
        
        public decimal CalculateCost(int zip, int length, int width, int height, int weight,
            string deliveryOption)
        {
            Log.Information("Calculating Cost of shipment...");
            // decimal residentialCost = CalculateResidentialCost(isResidential);

            decimal deliveryOptionsCost = CalculateDeliveryOptions(deliveryOption);
            decimal zipCost = CalculateZipCost(zip);
            decimal sizeCost = CalculatePackageSizeCost(length, width, height, weight);
            decimal sum = deliveryOptionsCost + zipCost + sizeCost;

            Log.Information("Total Shipment Cost is {0}", sum);
            return sum;
        }

        // Logic for is residential cost variable here
        public decimal CalculateResidentialCost(bool isResidential)
        {
            Log.Information("Calculating Residential variable cost...");
            if (isResidential)
            {
                Log.Information("Residential Cost Variable is {0}", 2.00);
                return 2.00m;
            }
            Log.Information("Residential Cost Variable is {0}", 4.00);
            return 4.00m;
        }

        // Logic for Delivery options cost variable here Options (Ground 4-5 days, Next Day, Standard 1-3 days)
        public decimal CalculateDeliveryOptions(string deliveryOption)
        {
            Log.Information("Calculating Residential variable cost...");
            if (deliveryOption == null) return 0m;
            else
            {
                switch (deliveryOption.ToLower())
                {
                    case "ground":
                        Log.Information("Delivery Options {0} Cost Variable is {1}", "Ground", 5.00);
                        return 5.00m;
                    case "standard":
                        Log.Information("Delivery Options {0} Cost Variable is {1}", "Standard", 10.00);
                        return 10.00m;
                    case "next day":
                        Log.Information("Delivery Options {0} Cost Variable is {1}", "Next Day", 15.00);
                        return 15.00m;
                }
            }
            return 0m;
        }

        // Logic for zip cost here
        public decimal CalculateZipCost(int zip)
        {
            Log.Information("Calculating Zip variable cost...");
            // For now a basic cost is spit out and we can get more complex later on if need be
            if (zip == 0) return 0m;
            else
            {
                Log.Information("Zip Cost Variable is {0}", 5.00);
                return 5.00m;
            }
        }

        // Logic for package dimensions cost variable here -> will check the packaging type for STANDARD sizes
        public decimal CalculatePackageSizeCost(int length, int width, 
            int height, int weight)
        {
            Log.Information("Calculating Package Size variable cost...");
            // If package is standard sizing
            /*if (isPackageStandard && packageSize != null)
            {
                switch (packageSize.ToLower())
                {
                    case "small":
                        return 5.00m;
                    case "medium":
                        return 10.00m;
                    case "large":
                        return 15.00m;
                }
            }*/
            // Else we will do the math for non standard sizing

            decimal lengthPrice = length / 2;
            decimal widthPrice = width / 2;
            decimal heightPrice = height / 2;
            decimal weightPrice = (weight + 5) / 2;
            decimal sum = lengthPrice + widthPrice + heightPrice + weightPrice;
            Log.Information("Delivery Size Cost Variable is {0}", sum);
            return sum;
        }

        // Method to determine the Packaging type
        // We may or may not need this
        public void PackageTypeSelection(bool packageType)
        {
            
            IsPackageStandard = packageType;
            if (IsPackageStandard)
            {
                // Logic to choose form that only allows user to select package size (Small, Medium, Large)
            }
            else
            {
                // Logic to choose form that allows user to enter custom dimensions for package
            }
        }
    }
}