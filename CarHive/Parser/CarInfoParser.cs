using System;
using System.Text.RegularExpressions;
using CarHive.Entity;

namespace CarHive.Parser;

public class CarInfoParser
{
    private static readonly Dictionary<string, string> Patterns = new Dictionary<string, string>
    {
        { "CarHiveBatchNumber", @"Car Hive Batch Number:\s*(.+)" },
        { "CarCode", @"Car Code:\s*(.+)" },
        { "CarTitle", @"Car Title:\s*(.+)" },
        { "SellerOwnerName", @"Seller / Owner Name:\s*(.+)" },
        { "Address", @"Address:\s*(.+)" },
        { "City", @"City:\s*(.+)" },
        { "State", @"State:\s*(.+)" },
        { "Pincode", @"Pincode:\s*(\d+)" },
        //{ "Pincode", @"Pincode:\s*(.+)" },
        { "ContactNumber", @"Contact Number:\s*(\d+)" },
        { "EmailAddress", @"Email Address:\s*(\S+)" },
        { "FuelType", @"Fuel Type:\s*(.+)" },
        { "Condition", @"Condition:\s*(.+)" },
        { "YearOfProduction", @"Year of Production:\s*(\d+)" },
        { "YearOfManufacturing", @"Year of Manufacturing:\s*(\d+)" },
        { "BodyType", @"Body Type:\s*(.+)" },
        { "Mileage", @"Mileage \(km\):\s*(\d+)" },
        { "Transmission", @"Transmission \(Manual/Automatic\):\s*(\w+)" },
        { "EngineCapacity", @"Engine Capacity \(cc\):\s*(\d+)" },
        { "Color", @"Color:\s*(.+)" },
        { "ColorCode", @"Color Code:\s*(.+)" },
        { "NumberOfOwners", @"Number of Owners:\s*(\d+)" },
        { "RegistrationCity", @"Registration City:\s*(.+)" },
        { "RegistrationNumber", @"Registration Number:\s*(.+)" },
        { "VIN", @"VIN:\s*(.+)" },
        { "ChassisNumber", @"Chassis Number:\s*(.+)" },
        { "InsuranceValidity", @"Insurance Validity:\s*(.+)" },
        { "RCStatus", @"RC Status:\s*(.+)" },
        { "ServiceHistory", @"Service History:\s*(.+)" },
        { "LastServiceDate", @"Last Service Date:\s*(.+)" },
        { "ServiceCenterHistory", @"Service Center History:\s*(.+)" },
        { "WarrantyStatus", @"Warranty Status:\s*(.+)" },
        { "FeatureHighlights", @"Feature Highlights:\s*(.+)" },
        { "CarAccessories", @"Car Accessories:\s*(.+)" },
        { "FuelEfficiency", @"Fuel Efficiency \(km/l\):\s*([\d\.]+)\s*km/l" },
        { "TyreCondition", @"Tyre Condition:\s*(.+)" },
        { "InteriorCondition", @"Interior Condition:\s*(.+)" },
        { "ExteriorCondition", @"Exterior Condition:\s*(.+)" },
        { "RoadTaxPaid", @"Road Tax Paid:\s*(.+)" },
        { "LoanStatus", @"Loan Status:\s*(.+)" },
        { "AskingPrice", @"Asking Price \(₹\):\s*([\d,]+)" },
        { "Negotiable", @"Negotiable \(Yes/No\):\s*(\w+)" },
        { "Description", @"Description:\s*(.+)" }
    };

    public CarInfo Parse(string input)
    {
        var carInfo = new CarInfo();
        foreach (var kvp in Patterns)
        {
            var match = Regex.Match(input, kvp.Value);
            if (match.Success)
            {
                typeof(CarInfo).GetProperty(kvp.Key)?.SetValue(carInfo, match.Groups[1].Value);
            }
        }
        return carInfo;
    }
}