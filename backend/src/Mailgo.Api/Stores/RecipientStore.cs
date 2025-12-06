// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mailgo.Api.Data;
using Mailgo.Api.Responses;
using Mailgo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Stores;

public class RecipientStore
{
    private readonly ApplicationDbContext _dbContext;

    public RecipientStore(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedResult<RecipientResponse>> GetRecipientsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Recipients.AsNoTracking().OrderByDescending(r => r.CreatedAt);
        var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RecipientResponse>(
            items.Select(r => r.ToResponse()).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<HashSet<string>> GetExistingRecipientEmailsAsync(CancellationToken cancellationToken)
    {
        var emails = await _dbContext.Recipients
            .AsNoTracking()
            .Select(r => r.Email.ToLowerInvariant())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new HashSet<string>(emails, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int> SaveRecipientsAsync(IEnumerable<Recipient> recipients, CancellationToken cancellationToken)
    {
        var list = recipients.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        await _dbContext.Recipients.AddRangeAsync(list, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return list.Count;
    }
}
