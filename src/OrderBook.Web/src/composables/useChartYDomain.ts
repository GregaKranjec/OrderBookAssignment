import { computed, ref, watch } from 'vue'

// Grows y axis domain in fixed increments
export function useChartYDomain(maximumQuantity: () => number, step: number) {
    const upperBound = ref(step)

    watch(maximumQuantity, maximum => {
        if (!Number.isFinite(maximum) || maximum <= upperBound.value) {
            return
        }

        upperBound.value = Math.ceil(maximum / step) * step
    }, { immediate: true })

    return computed<[number, number]>(() => [0, upperBound.value])
}
