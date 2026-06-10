import assert from 'node:assert/strict';
import test from 'node:test';

import {
  compareTableMinWidth,
  BENCHMARK_OPTIONS,
  formatCompareNumber,
  formatComparePercent,
  formatCompareVolume,
  MAX_COMPARE_FIBRAS,
  MIN_COMPARE_FIBRAS,
  normalizeCompareBenchmarks,
  normalizeCompareTickers,
  parseCompareTickers,
  parseCompareBenchmarks,
  serializeCompareTickers,
  serializeCompareBenchmarks,
} from './comparador-logic.ts';

test('comparador logic exports the expected helpers', () => {
  assert.equal(MIN_COMPARE_FIBRAS, 2);
  assert.equal(MAX_COMPARE_FIBRAS, 4);
  assert.equal(typeof parseCompareTickers, 'function');
  assert.equal(typeof normalizeCompareTickers, 'function');
  assert.equal(typeof serializeCompareTickers, 'function');
  assert.equal(typeof formatCompareNumber, 'function');
  assert.equal(typeof formatComparePercent, 'function');
  assert.equal(typeof formatCompareVolume, 'function');
  assert.equal(typeof compareTableMinWidth, 'function');
  assert.deepEqual(BENCHMARK_OPTIONS, ['ipc', 'sp500']);
  assert.equal(typeof parseCompareBenchmarks, 'function');
  assert.equal(typeof normalizeCompareBenchmarks, 'function');
  assert.equal(typeof serializeCompareBenchmarks, 'function');
});

test('comparador logic handles empty input defensively', () => {
  assert.deepEqual(parseCompareTickers(''), []);
  assert.deepEqual(normalizeCompareTickers([]), []);
  assert.equal(serializeCompareTickers([]), '');
});

test('comparador logic normalizes benchmark filters', () => {
  assert.deepEqual(parseCompareBenchmarks('ipc,sp500,ipc'), ['ipc', 'sp500']);
  assert.deepEqual(normalizeCompareBenchmarks(['IPC', 'sp500', 'foo']), ['ipc', 'sp500']);
  assert.equal(serializeCompareBenchmarks(['sp500', 'ipc', 'sp500']), 'sp500,ipc');
});
