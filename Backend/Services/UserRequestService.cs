using Backend.Models;
using System.Collections.Concurrent;

namespace Backend.Services;

public class UserRequestService
{
    private static readonly IDictionary<string, UserRequest> userRequests = new ConcurrentDictionary<string, UserRequest>();

    public UserRequest GetUserRequest(String requestId)
    {
        if (!userRequests.TryGetValue(requestId, out var userRequest))
        {
            userRequest = new();
            userRequests.Add(requestId, userRequest);
        }
        return userRequest;
    }
}
