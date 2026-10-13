using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record RoomResponse
    (
        string RoomName,
        string RoomType
    );
}
