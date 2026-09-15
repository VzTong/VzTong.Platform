using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;

namespace Platform.Identity.Identity;

public class AssignUserRoleOnCreateHandler
    : ILocalEventHandler<EntityCreatedEventData<IdentityUser>>,
      ITransientDependency
{
    private readonly IdentityUserManager _userManager;

    public AssignUserRoleOnCreateHandler(IdentityUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task HandleEventAsync(EntityCreatedEventData<IdentityUser> eventData)
    {
        var user = eventData.Entity;

        if (await _userManager.IsInRoleAsync(user, "admin"))
        {
            return;
        }

        if (await _userManager.IsInRoleAsync(user, "User"))
        {
            return;
        }

        await _userManager.AddToRoleAsync(user, "User");
    }
}