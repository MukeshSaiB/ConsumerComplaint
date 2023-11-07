namespace ConsumerComplaint.Models
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    public class Company
    {
        public int CompanyID { get; set; }

        public string CompanyName { get; set; }

        // Navigation property to represent the one-to-many relationship with Product
        public virtual ICollection<Product> Products { get; set; }

        
    }

    public class Product
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }

        // Foreign key to Company
        [ForeignKey("Company")]
        public int CompanyID { get; set; }

        // Navigation property back to Company
        public virtual Company Company { get; set; }

        // Navigation property to represent the one-to-many relationship with Complaint
        public virtual ICollection<Complaint> Complaints { get; set; }


    }

    public class Complaint
    {
        public int ComplaintID { get; set; }

        public string IssueDescription { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateReceived { get; set; }

        // Foreign key to Product
        [ForeignKey("Product")]
        public int ProductID { get; set; }

        // Navigation property back to Product
        public virtual Product Product { get; set; }
    }

    // Models to match the JSON response from the API
    public class ComplaintApiResponse
    {
        [JsonPropertyName("hits")]
        public Hits Hits { get; set; }
    }

    public class Hits
    {
        [JsonPropertyName("hits")]
        public List<ComplaintRecord> ComplaintRecords { get; set; }
    }

    public class ComplaintRecord
    {
        [JsonPropertyName("_source")]
        public ComplaintSource Source { get; set; }
    }

    public class ComplaintSource
    {
        [JsonPropertyName("complaint_id")]
        public int complaint_id { get; set; }

        [JsonPropertyName("product")]
        public string ProductName { get; set; }
        [JsonPropertyName("company")]
        public string CompanyName { get; set; }

        [JsonPropertyName("issue")]
        public string Issue { get; set; }

        [JsonPropertyName("date_received")]
        public DateTime DateReceived { get; set; }
    }

    public class ComplaintViewModel
    {
        public int ComplaintID { get; set; } // Added ComplaintID
        public DateTime DateReceived { get; set; }
        public string ProductName { get; set; }
        public string IssueDescription { get; set; }
        public string CompanyName { get; set; }
    }

    public class ComplaintAddViewModel
    {
        public string IssueDescription { get; set; }
        public DateTime DateReceived { get; set; }
        public int ProductID { get; set; }
        public int CompanyID { get; set; } // Include CompanyID to bind it directly

        public SelectList ProductList { get; set; }
        public SelectList CompanyList { get; set; }
    }
}
