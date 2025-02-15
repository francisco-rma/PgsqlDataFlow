using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Benchmarker
{
    [Table("ft_queue")]
    public class FtQueue

    {
        [Key]
        [Column("id_ft_queue")]
        [Name("id_ft_queue")]
        public long idFtQueue { get; set; }

        [Column("id_queue")]
        [Name("id_queue")]
        public long idQueue { get; set; }

        [Column("id_health_unit")]
        [Name("id_health_unit")]
        public int idHealthUnit { get; set; }

        [Column("id_module")]
        [Name("id_module")]
        public int? idModule { get; set; }

        [Column("id_parent_service")]
        [Name("id_parent_service")]
        public int? idParentService { get; set; }

        [Column("id_service")]
        [Name("id_service")]
        public int? idService { get; set; }

        [Column("id_sector")]
        [Name("id_sector")]
        public int? idSector { get; set; }

        [Column("id_room")]
        [Name("id_room")]
        public int? idRoom { get; set; }

        [Column("id_episode")]
        [Name("id_episode")]
        public int idEpisode { get; set; }

        [Column("id_nature_of_attendance")]
        [Name("id_nature_of_attendance")]
        public short? idNatureOfAttendance { get; set; }

        [Column("is_priority")]
        [Name("is_priority")]
        public bool isPriority { get; set; }

        [Column("id_care_line_priority")]
        [Name("id_care_line_priority")]
        public int? idCareLinePriority { get; set; }

        [Column("id_flow")]
        [Name("id_flow")]
        public int? idFlow { get; set; }

        [Column("id_user_attendant")]
        [Name("id_user_attendant")]
        public int? idUserAttendant { get; set; }

        [Column("id_cbo")]
        [Name("id_cbo")]
        public int? idCbo { get; set; }

        //[Column("id_profession")]
        //[NameColumn("id_profession")]
        //public int? idProfession { get; set; }

        [Column("is_reclassification")]
        [Name("is_reclassification")]
        public bool isReclassification { get; set; }

        [Column("id_patient")]
        [Name("id_patient")]
        public int? idPatient { get; set; }

        [Column("patient_age_days")]
        [Name("patient_age_days")]
        public int? patientAgeDays { get; set; }

        [Column("id_gender")]
        [Name("id_gender")]
        public int? idGender { get; set; }

        [Column("id_arrival_reason")]
        [Name("id_arrival_reason")]
        public int? idArrivalReason { get; set; }

        [Column("id_gravity")]
        [Name("id_gravity")]
        public short? idGravity { get; set; }

        [Column("id_flowchart")]
        [Name("id_flowchart")]
        public int? idFlowchart { get; set; }

        [Column("id_dm_forward")]
        [Name("id_dm_forward")]
        public int? idDmForward { get; set; }

        [Column("id_arrival_type")]
        [Name("id_arrival_type")]
        public int? idArrivalType { get; set; }

        [Column("id_discriminator")]
        [Name("id_discriminator")]
        public int? idDiscriminator { get; set; }

        [Column("id_origin")]
        [Name("id_origin")]
        public int? idOrigin { get; set; }

        [Column("id_suspicion")]
        [Name("id_suspicion")]
        public int? idSuspicion { get; set; }

        [Column("has_reevaluation_medic")]
        [Name("has_reevaluation_medic")]
        public bool hasReevaluationMedic { get; set; }

        [Column("door_medic_time")]
        [Name("door_medic_time")]
        public decimal? doorMedicTime { get; set; }

        [Column("reevaluation_time")]
        [Name("reevaluation_time")]
        public decimal? reevaluationTime { get; set; }

        [Column("decision_time")]
        [Name("decision_time")]
        public decimal? decisionTime { get; set; }

        [Column("is_first_medic")]
        [Name("is_first_medic")]
        public bool isFirstMedic { get; set; }

        [Column("datetime_inclusion")]
        [Name("datetime_inclusion")]
        public DateTime datetimeInclusion { get; set; }

        [Column("datetime_start_attendance")]
        [Name("datetime_start_attendance")]
        public DateTime? datetimeStartAttendance { get; set; }

        [Column("datetime_out")]
        [Name("datetime_out")]
        public DateTime? datetimeOut { get; set; }

        [Column("time_wait")]
        [Name("time_wait")]
        public decimal? timeWait { get; set; }

        [Column("time_attendance")]
        [Name("time_attendance")]
        public decimal? timeAttendance { get; set; }

        [Column("id_status_queue")]
        [Name("id_status_queue")]
        public int? idStatusQueue { get; set; }

        [Column("type_evasion")]
        [Name("type_evasion")]
        public int? typeEvasion { get; set; }

        [Column("was_called_panel")]
        [Name("was_called_panel")]
        public bool wasCalledPanel { get; set; }

        [Column("id_user_evasion")]
        [Name("id_user_evasion")]
        public int? idUserEvasion { get; set; }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member