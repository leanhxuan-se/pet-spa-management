using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record RoomResponse
    (
        long Id,
        string RoomName,
        string RoomType
    );
}
