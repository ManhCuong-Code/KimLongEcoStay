using System;
using System.Collections.Generic;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class HeritageLocation
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Quần Thể Di Tích", "Làng Nghề Truyền Thống", "Chùa Cổ & Tâm Linh", "Nhà Vườn Di Sản", "Trụ Sở Homestay"
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceFromEcoStayKm { get; set; }
        public string TravelEstimate { get; set; } = string.Empty; // Ví dụ: "850m - 3 phút đạp xe Eco-Bike"
        public string ImageUrl { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ExperienceHighlights { get; set; } = new();
        public string OpeningHours { get; set; } = "07:30 - 17:30";
        public string TicketInfo { get; set; } = "Miễn phí tham quan";
        public string GoogleMapsQueryUrl => $"https://www.google.com/maps/search/?api=1&query={Latitude},{Longitude}";
        public string GoogleMapsDirectionUrl => $"https://www.google.com/maps/dir/?api=1&origin=16.4560,107.5495&destination={Latitude},{Longitude}&travelmode=bicycling";
        public bool IsEcoStayCenter { get; set; } = false;
        public string BadgeColor { get; set; } = "success";
        public string IconClass { get; set; } = "bi-geo-alt-fill";
    }

    public class HeritageTourRoute
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Duration { get; set; } = "Nửa ngày (3 - 4 giờ)";
        public string Transport { get; set; } = "Xe đạp trợ lực điện Eco-Bike";
        public string Description { get; set; } = string.Empty;
        public List<string> StopLocationIds { get; set; } = new();
        public string Badge { get; set; } = "Khuyên dùng";
    }
}
