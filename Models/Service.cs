using AKUNAeat.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace AKUNAeat.Models
{
    public enum ServiceType
    {
        Lunch,
        Dinner
    }
    public class Service
    {
        private int serviceId;
        private TimeSpan endTime;
        private TimeSpan startTime;
        private ServiceType serviceType;

        public int ServiceId
        {
            get { return serviceId; }
            set { serviceId = value; }
        }

        [Required(ErrorMessage = "Le champ 'Heure de début' est requis.")]
        [DataType(DataType.Time)]
        [Display(Name = "Heure de début")]
        public TimeSpan StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        [Required(ErrorMessage = "Le champ 'Heure de fin' est requis.")]
        [DataType(DataType.Time)]
        [Display(Name = "Heure de fin")]
        public TimeSpan EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        [Required(ErrorMessage = "Le champ 'Type de service' est requis.")]
        [Display(Name = "Type de service")]
        public ServiceType ServiceType
        {
            get { return serviceType; }
            set { serviceType = value; }
        }

        public Service() { }

        public Service(int serviceId,TimeSpan startTime, TimeSpan endTime, ServiceType serviceType)
        {
            this.serviceId = serviceId;
            this.startTime = startTime;
            this.endTime = endTime;
            this.serviceType = serviceType;
        }

        public override string ToString()
        {
            return $"{ServiceType} - {StartTime} à {EndTime}";
        }

        public static async Task<List<Service>> GetServicesByRestaurantId(int restaurantId, IRestaurantDAL dal)
        {
            return await dal.GetServicesByRestaurantIdAsync(restaurantId);
        }

        public async Task<bool> AddServiceAsync(int restaurantId, IRestaurantDAL dal)
        {
            return await dal.InsertServiceAsync(this, restaurantId);
        }

    }
}
