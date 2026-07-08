import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ShipmentApiService } from '../../data-access/shipment-api.service';
import { CreateShipmentRequest, ShipmentRow } from '../../models/shipment.models';
import { ShipmentsPage } from './shipments-page';

const STATUS_AUTO_REFRESH_INTERVAL_MS = 2500;

describe('ShipmentsPage auto-refresh', () => {
  let fixture: ComponentFixture<ShipmentsPage>;
  let api: FakeShipmentApiService;

  beforeEach(async () => {
    vi.useFakeTimers();
    api = new FakeShipmentApiService();

    await TestBed.configureTestingModule({
      imports: [ShipmentsPage],
      providers: [{ provide: ShipmentApiService, useValue: api }],
    }).compileComponents();

    fixture = TestBed.createComponent(ShipmentsPage);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    vi.clearAllTimers();
    vi.useRealTimers();
  });

  it('auto-refreshes a created pending shipment and stops on a terminal status', async () => {
    api.createShipmentResponses.push(shipment('shipment-1', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-1', shipment('shipment-1', 'Reserved'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();

    expect(pageText()).toContain('ReservationRequested');

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-1']);
    expect(pageText()).toContain('Reserved');

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-1']);
  });

  it('starts auto-refresh for a read-by-id pending shipment', async () => {
    api.enqueueGetShipment('shipment-2', shipment('shipment-2', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-2', shipment('shipment-2', 'Reserved'));

    page().shipmentIdToLoad = 'shipment-2';
    await page().loadShipment();
    await settle();

    expect(pageText()).toContain('Loaded shipment');
    expect(pageText()).toContain('ReservationRequested');

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-2', 'shipment-2']);
    expect(pageText()).toContain('Reserved');
  });

  it('does not auto-refresh a read-by-id non-pending shipment', async () => {
    api.enqueueGetShipment('shipment-3', shipment('shipment-3', 'Reserved'));

    page().shipmentIdToLoad = 'shipment-3';
    await page().loadShipment();
    await settle();

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-3']);
    expect(pageText()).toContain('Reserved');
  });

  it('does not overlap auto-refresh with an in-flight manual refresh', async () => {
    const manualRefresh = new Deferred<ShipmentRow>();
    api.createShipmentResponses.push(shipment('shipment-4', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-4', manualRefresh);

    await page().submitCreateShipment(createShipmentRequest());
    await settle();
    const manualRefreshPromise = page().refreshActiveShipmentStatus();
    await settle();

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-4']);

    manualRefresh.resolve(shipment('shipment-4', 'Reserved'));
    await manualRefreshPromise;
    await settle();

    expect(pageText()).toContain('Reserved');
  });

  it('does not let a stale refresh overwrite a newer active shipment', async () => {
    const staleRefresh = new Deferred<ShipmentRow>();
    api.createShipmentResponses.push(shipment('shipment-old', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-old', staleRefresh);
    api.enqueueGetShipment('shipment-new', shipment('shipment-new', 'Reserved'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();
    const staleRefreshPromise = page().refreshActiveShipmentStatus();
    await settle();

    page().shipmentIdToLoad = 'shipment-new';
    await page().loadShipment();
    await settle();

    expect(pageText()).toContain('shipment-new');

    staleRefresh.resolve(shipment('shipment-old', 'Reserved'));
    await staleRefreshPromise;
    await settle();

    expect(pageText()).toContain('shipment-new');
    expect(pageText()).not.toContain('shipment-old');
  });

  it('does not let a stale refresh error stop polling for a newer active shipment', async () => {
    const staleRefresh = new Deferred<ShipmentRow>();
    api.createShipmentResponses.push(shipment('shipment-old', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-old', staleRefresh);
    api.enqueueGetShipment('shipment-new', shipment('shipment-new', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-new', shipment('shipment-new', 'Reserved'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();
    const staleRefreshPromise = page().refreshActiveShipmentStatus();
    await settle();

    page().shipmentIdToLoad = 'shipment-new';
    await page().loadShipment();
    await settle();

    staleRefresh.reject(new Error('Old shipment failed.'));
    await staleRefreshPromise;
    await settle();

    expect(pageText()).toContain('shipment-new');
    expect(pageText()).not.toContain('Old shipment failed.');

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-old', 'shipment-new', 'shipment-new']);
    expect(pageText()).toContain('Reserved');
  });

  it('stops auto-refresh after the bounded attempt limit', async () => {
    api.createShipmentResponses.push(shipment('shipment-5', 'ReservationRequested'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();

    for (let attempt = 0; attempt < 6; attempt += 1) {
      api.enqueueGetShipment('shipment-5', shipment('shipment-5', 'ReservationRequested'));
      await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
      await settle();
    }

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual([
      'shipment-5',
      'shipment-5',
      'shipment-5',
      'shipment-5',
      'shipment-5',
      'shipment-5',
    ]);
  });

  it('stops auto-refresh and shows an error when refresh fails', async () => {
    api.createShipmentResponses.push(shipment('shipment-6', 'ReservationRequested'));
    api.enqueueGetShipment('shipment-6', new Error('Gateway failed.'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(pageText()).toContain('Gateway failed.');

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual(['shipment-6']);
  });

  it('cleans up auto-refresh timers on destroy', async () => {
    api.createShipmentResponses.push(shipment('shipment-7', 'ReservationRequested'));

    await page().submitCreateShipment(createShipmentRequest());
    await settle();
    fixture.destroy();

    await vi.advanceTimersByTimeAsync(STATUS_AUTO_REFRESH_INTERVAL_MS);
    await settle();

    expect(api.getShipmentCalls).toEqual([]);
  });

  function page(): ShipmentsPageTestHarness {
    return fixture.componentInstance as unknown as ShipmentsPageTestHarness;
  }

  async function settle(): Promise<void> {
    for (let attempt = 0; attempt < 20; attempt += 1) {
      await Promise.resolve();
    }

    fixture.detectChanges();
  }

  function pageText(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }
});

interface ShipmentsPageTestHarness {
  shipmentIdToLoad: string;
  loadShipment(): Promise<void>;
  refreshActiveShipmentStatus(): Promise<void>;
  submitCreateShipment(request: CreateShipmentRequest): Promise<void>;
}

class FakeShipmentApiService {
  readonly createShipmentResponses: ShipmentResponse[] = [];
  readonly getShipmentCalls: string[] = [];
  private readonly getShipmentResponses = new Map<string, ShipmentResponse[]>();

  async createShipment(_request: CreateShipmentRequest): Promise<ShipmentRow> {
    return resolveShipment(this.createShipmentResponses.shift() ?? shipment('created-shipment', 'Reserved'));
  }

  async getShipment(shipmentId: string): Promise<ShipmentRow> {
    this.getShipmentCalls.push(shipmentId);
    return resolveShipment(this.getShipmentResponses.get(shipmentId)?.shift() ?? shipment(shipmentId, 'ReservationRequested'));
  }

  enqueueGetShipment(shipmentId: string, response: ShipmentResponse): void {
    const responses = this.getShipmentResponses.get(shipmentId) ?? [];
    responses.push(response);
    this.getShipmentResponses.set(shipmentId, responses);
  }
}

type ShipmentResponse = ShipmentRow | Error | Deferred<ShipmentRow>;

class Deferred<T> {
  readonly promise: Promise<T>;
  private resolveValue?: (value: T) => void;
  private rejectValue?: (error: Error) => void;

  constructor() {
    this.promise = new Promise<T>((resolve, reject) => {
      this.resolveValue = resolve;
      this.rejectValue = reject;
    });
  }

  resolve(value: T): void {
    this.resolveValue?.(value);
  }

  reject(error: Error): void {
    this.rejectValue?.(error);
  }
}

async function resolveShipment(response: ShipmentResponse): Promise<ShipmentRow> {
  if (response instanceof Deferred) {
    return response.promise;
  }

  if (response instanceof Error) {
    throw response;
  }

  return response;
}

function shipment(shipmentId: string, status: string): ShipmentRow {
  return {
    shipmentId,
    shipmentNumber: shipmentId,
    status,
    items: [],
  };
}

function createShipmentRequest(): CreateShipmentRequest {
  return {
    senderCompanyId: 'sender-company',
    senderAddressId: 'sender-address',
    receiverCompanyId: 'receiver-company',
    receiverAddressId: 'receiver-address',
    items: [{ sku: 'SKU-1', quantity: 1 }],
  };
}
