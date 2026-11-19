using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record CreateRoomRequest(
        string RoomName,
        string RoomType
        );
}
