import { toCompanyAddressRows, toCompanyPageResult, toCompanyRow } from './company-api.mapper';

describe('company API mapper', () => {
  it('maps paged company responses and backend field aliases', () => {
    const result = toCompanyPageResult(
      {
        items: [
          {
            companyId: 'company-1',
            code: 'C-001',
            companyName: 'Acme Logistics',
            status: 'Active',
            createdAtUtc: '2026-01-01T00:00:00Z',
            updatedAtUtc: '2026-01-02T00:00:00Z',
          },
        ],
        pageNumber: 2,
        pageSize: 20,
        hasMore: true,
      },
      1
    );

    expect(result).toEqual({
      items: [
        {
          id: 'company-1',
          externalCode: 'C-001',
          name: 'Acme Logistics',
          status: 'Active',
          createdAtUtc: '2026-01-01T00:00:00Z',
          updatedAtUtc: '2026-01-02T00:00:00Z',
        },
      ],
      pageNumber: 2,
      pageSize: 20,
      hasMore: true,
    });
  });

  it('falls back to requested page, item count, and no more pages when paging metadata is missing', () => {
    const result = toCompanyPageResult(
      {
        items: [{ id: 'company-1', externalCode: 'C-001', name: 'Acme Logistics' }],
      },
      3
    );

    expect(result.pageNumber).toBe(3);
    expect(result.pageSize).toBe(1);
    expect(result.hasMore).toBe(false);
  });

  it('maps a created company row from primary backend fields', () => {
    expect(toCompanyRow({ id: 'company-2', externalCode: 'C-002', name: 'Northwind', status: 'Active' })).toEqual({
      id: 'company-2',
      externalCode: 'C-002',
      name: 'Northwind',
      status: 'Active',
      createdAtUtc: undefined,
      updatedAtUtc: undefined,
    });
  });

  it('maps address arrays and address aliases', () => {
    expect(
      toCompanyAddressRows([
        {
          addressId: 'address-1',
          type: 'Shipping',
          countryCode: 'GB',
          city: 'London',
          postalCode: 'SW1A 1AA',
          line1: '1 Warehouse Road',
          line2: 'Dock 4',
        },
      ])
    ).toEqual([
      {
        id: 'address-1',
        addressType: 'Shipping',
        countryCode: 'GB',
        city: 'London',
        postalCode: 'SW1A 1AA',
        line1: '1 Warehouse Road',
        line2: 'Dock 4',
        createdAtUtc: undefined,
        updatedAtUtc: undefined,
      },
    ]);
  });

  it('returns an empty address list for non-array payloads', () => {
    expect(toCompanyAddressRows(null)).toEqual([]);
  });
});
