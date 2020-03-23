﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicArrivals.Models
{
    public class VideoOpenVidu : IVideoConferenceManager
    {
        private Guid systemId;

        public void Initialize(Settings settings)
        {
            systemId = settings.SystemIdentifier;
        }

        /// <summary>
        /// Get URL for conference
        /// </summary>
        /// <param name="id">The id of the appointment (unique ==> Appointment Resource id)</param>
        public String getConferenceUrl(String appointmentId)
        {
            return "https://demos.openvidu.io/openvidu-call/#/" + systemId.ToString()+"-"+appointmentId;
        }

        /// <summary>
        /// Return true if it's possible to know if the patient has joined (not always possible with video services)
        /// </summary>
        /// <param name="id">The id of the appointment (unique ==> Appointment Resource id)</param>
        public Boolean canKnowIfJoined()
        {
            return false;
        }

        /// <summary>
        /// Return true if someone (assumed to be the patient) has joined the conference call
        /// </summary>
        /// <param name="id">The id of the appointment (unique ==> Appointment Resource id)</param>
        public Boolean hasSomeoneJoined(String appointmentId)
        {
            return false;
        }
    }
}