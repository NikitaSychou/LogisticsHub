export interface CompanyRow {
  id?: string;
  externalCode?: string;
  name?: string;
  status?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  raw: unknown;
}

export interface CompanyAddressRow {
  id?: string;
  addressType?: string;
  countryCode?: string;
  city?: string;
  postalCode?: string;
  line1?: string;
  line2?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}

export interface CreateCompanyRequest {
  name: string;
  externalCode: string | null;
  status: string;
}

export interface CreateCompanyAddressRequest {
  addressType: string;
  countryCode: string;
  city: string;
  postalCode: string | null;
  line1: string;
  line2: string | null;
}
