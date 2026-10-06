import { computed, ref, watch } from 'vue'

// Grows in fixed increments during updates, with an explicit reset for range changes.
export function useChartYDomain(maximumQuantity: () => number, step: number) {
    const upperBound = ref(step)

    function calculateUpperBound(maximum: number) {
        return Number.isFinite(maximum)
            ? Math.max(step, Math.ceil(maximum / step) * step)
            : step
    }

    watch(maximumQuantity, maximum => {
        upperBound.value = Math.max(upperBound.value, calculateUpperBound(maximum))
    }, { immediate: true })

    function resetYDomain() {
        upperBound.value = calculateUpperBound(maximumQuantity())
    }

    return {
        yDomain: computed<[number, number]>(() => [0, upperBound.value]),
        resetYDomain,
    }
}
