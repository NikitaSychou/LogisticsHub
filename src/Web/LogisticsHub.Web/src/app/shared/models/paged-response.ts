export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}
