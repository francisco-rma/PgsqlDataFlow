using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Benchmarker
{
    [Table("ft_queue")]
    public class FtQueue

    {
        public FtQueue()
        {
            idQueue = Random.Shared.NextInt64();
            idHealthUnit = Random.Shared.Next();
            idModule = Random.Shared.Next();
            idParentService = Random.Shared.Next();
            idService = Random.Shared.Next();
            idSector = Random.Shared.Next();
            idRoom = Random.Shared.Next();
            idEpisode = Random.Shared.Next();
            idNatureOfAttendance = (short)Random.Shared.Next();
            isPriority = false;
            idCareLinePriority = Random.Shared.Next();
            idFlow = Random.Shared.Next();
            idUserAttendant = Random.Shared.Next();
            idCbo = Random.Shared.Next();
            isReclassification = false;
            idPatient = Random.Shared.Next();
            patientAgeDays = (short)Random.Shared.Next();
            idGender = Random.Shared.Next();
            idArrivalReason = Random.Shared.Next();
            idGravity = (short)Random.Shared.Next();
            idFlowchart = Random.Shared.Next();
            idDmForward = Random.Shared.Next();
            idArrivalType = Random.Shared.Next();
            idDiscriminator = Random.Shared.Next();
            idOrigin = Random.Shared.Next();
            idSuspicion = Random.Shared.Next();
            hasReevaluationMedic = false;
            doorMedicTime = (decimal)Random.Shared.NextDouble();
            reevaluationTime = (decimal)Random.Shared.NextDouble();
            decisionTime = (decimal)Random.Shared.NextDouble();
            
            datetimeInclusion = DateTime.UtcNow;
            //datetimeInclusion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            
            datetimeStartAttendance = new DateTime(Random.Shared.Next(), DateTimeKind.Utc);
            //datetimeStartAttendance = new DateTime(Random.Shared.Next(), DateTimeKind.Unspecified);
            
            datetimeOut = new DateTime(Random.Shared.Next(), DateTimeKind.Utc);
            //datetimeOut = new DateTime(Random.Shared.Next(), DateTimeKind.Unspecified);
            
            isFirstMedic = false;
            timeWait = 0;
            timeAttendance = 0;
            idStatusQueue = Random.Shared.Next();
            typeEvasion = 1;
            wasCalledPanel = false;
            idUserEvasion = Random.Shared.Next();
        }

        [Key]
        [Column("id_ft_queue")]
        public long idFtQueue { get; set; }

        [Column("id_queue")]
        public long idQueue { get; set; }

        [Column("id_health_unit")]
        public int idHealthUnit { get; set; }

        [Column("id_module")]
        public int? idModule { get; set; }

        [Column("id_parent_service")]
        public int? idParentService { get; set; }

        [Column("id_service")]
        public int? idService { get; set; }

        [Column("id_sector")]
        public int? idSector { get; set; }

        [Column("id_room")]
        public int? idRoom { get; set; }

        [Column("id_episode")]
        public int idEpisode { get; set; }

        [Column("id_nature_of_attendance")]
        public short? idNatureOfAttendance { get; set; }

        [Column("is_priority")]
        public bool isPriority { get; set; }

        [Column("id_care_line_priority")]
        public int? idCareLinePriority { get; set; }

        [Column("id_flow")]
        public int? idFlow { get; set; }

        [Column("id_user_attendant")]
        public int? idUserAttendant { get; set; }

        [Column("id_cbo")]
        public int? idCbo { get; set; }

        [Column("is_reclassification")]
        public bool isReclassification { get; set; }

        [Column("id_patient")]
        public int? idPatient { get; set; }

        [Column("patient_age_days")]
        public int? patientAgeDays { get; set; }

        [Column("id_gender")]
        public int? idGender { get; set; }

        [Column("id_arrival_reason")]
        public int? idArrivalReason { get; set; }

        [Column("id_gravity")]
        public short? idGravity { get; set; }

        [Column("id_flowchart")]
        public int? idFlowchart { get; set; }

        [Column("id_dm_forward")]
        public int? idDmForward { get; set; }

        [Column("id_arrival_type")]
        public int? idArrivalType { get; set; }

        [Column("id_discriminator")]
        public int? idDiscriminator { get; set; }

        [Column("id_origin")]
        public int? idOrigin { get; set; }

        [Column("id_suspicion")]
        public int? idSuspicion { get; set; }

        [Column("has_reevaluation_medic")]
        public bool hasReevaluationMedic { get; set; }

        [Column("door_medic_time")]
        public decimal? doorMedicTime { get; set; }

        [Column("reevaluation_time")]
        public decimal? reevaluationTime { get; set; }

        [Column("decision_time")]
        public decimal? decisionTime { get; set; }

        [Column("is_first_medic")]
        public bool isFirstMedic { get; set; }

        [Column("datetime_inclusion")]
        public DateTime datetimeInclusion { get; set; }

        [Column("datetime_start_attendance")]
        public DateTime? datetimeStartAttendance { get; set; }

        [Column("datetime_out")]
        public DateTime? datetimeOut { get; set; }

        [Column("time_wait")]
        public decimal? timeWait { get; set; }

        [Column("time_attendance")]
        public decimal? timeAttendance { get; set; }

        [Column("id_status_queue")]
        public int? idStatusQueue { get; set; }

        [Column("type_evasion")]
        public int? typeEvasion { get; set; }

        [Column("was_called_panel")]
        public bool wasCalledPanel { get; set; }

        [Column("id_user_evasion")]
        public int? idUserEvasion { get; set; }
    }


    public class TestDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
        public DbSet<FtQueue> ftQueue { get; set; }
    }
}