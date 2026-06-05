import assert from 'node:assert/strict';
import test from 'node:test';

import {
  compareTableMinWidth,
  formatCompareNumber,
  formatComparePercent,
  formatCompareVolume,
  MAX_COMPARE_FIBRAS,
  MIN_COMPARE_FIBRAS,
  normalizeCompareTickers,
  parseCompareTickers,
  serializeCompareTickers,
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
});

test('comparador logic handles empty input defensively', () => {
  assert.deepEqual(parseCompareTickers(''), []);
  assert.deepEqual(normalizeCompareTickers([]), []);
  assert.equal(serializeCompareTickers([]), '');
});
